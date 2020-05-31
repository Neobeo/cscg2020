<Query Kind="Statements">
  <Namespace>System.Net.Sockets</Namespace>
  <Namespace>System.Security.Cryptography</Namespace>
</Query>

var delay1 = TimeSpan.FromMilliseconds(15); // at regular positions
var delay2 = TimeSpan.FromMilliseconds(30); // at checkpoints

var name = "Neobeo";
var secretStr = @"1L3Z6NH40IKZJR63";
var secret = SHA256.Create().ComputeHash(Encoding.ASCII.GetBytes(secretStr))[..8];
var loginPacket = new byte[] { 0, 0, 0x4C }.Concat(secret).Concat(new[] { (byte)name.Length }).Concat(Encoding.ASCII.GetBytes(name)).Concat(new byte[32 - name.Length]).ToArray();

var client = new UdpClient("maze.liveoverflow.com", 1342);
client.Send(loginPacket, loginPacket.Length);

var task = client.ReceiveAsync();
if (!task.Wait(5000))
{
    Console.WriteLine("Timed out at start.");
    return;
}

var result = task.Result.Buffer;
switch (result[0] ^ result[2])
{
    case 0x4C: Console.WriteLine("We're in."); break;
    case 0x59: Console.WriteLine("Already logged in."); return;
    default: Console.WriteLine("Weird unknown login response."); return;
}

task = client.ReceiveAsync();
if (!task.Wait(1000))
{
    Console.WriteLine("Timed out waiting for spawn position.");
    return;
}
result = task.Result.Buffer;
for (int i = 2; i < result.Length; i++)
{
    result[i] ^= result[0];
    result[0] = (byte)((result[0] + result[1]) % 255);
}
// 99-5D-54-01-14-FA-25-02-22-51-05-03-1D-5A-1A-7D
//             <----x----> <----y----> <----z---->
//Console.WriteLine(BitConverter.ToString(result));
if (result[2] != 0x54)
{
    Console.WriteLine("Got unexpected packet at the start.");
    return;
}
var startx = BitConverter.ToInt32(result, 4);
var startz = BitConverter.ToInt32(result, 12);
Console.WriteLine($"Spawned at ({startx}, {startz})");



var poses = new[]
{
	// initial setup
    (2635000, 2297500), (2535000, 2297500), (2455000, 2277500), (2378350, 2237500), (2301700, 2177500), (2225050, 2117500), (2148400, 2057500), (2071750, 1997500),
	// checkpoints and intermediate steps
    (1995100, 1937500), (1917050, 1875000),
    (1839000, 1812500), (1840000, 1907500), (1810000, 1997500),
    (1765000, 2072500), (1765000, 2162500), (1795000, 2252500),
    (1855000, 2327500), (1765000, 2342500),
    (1675000, 2312500), (1615000, 2237500), (1585000, 2147500), (1555000, 2057500), (1540000, 1967500),
    (1540000, 1877500), (1615000, 1832500), (1675000, 1757500), (1735000, 1682500),
    (1780000, 1607500), (1765000, 1517500), (1765000, 1427500), (1705000, 1352500), (1690000, 1262500),
    (1630000, 1187500), (1540000, 1157500), (1465000, 1097500), (1375000, 1067500), (1300000, 1007500),
    (1210000, 992500), (1180000, 1082500), (1180000, 1172500),
    (1180000, 1262500), (1165000, 1361300), (1165000, 1451300), (1165000, 1551300), (1165000, 1651300), (1165000, 1751300), (1135000, 1844400),
    (1105000, 1937500), (1015700, 1982500), (940000, 2047800), (865000, 2072500),
    (780000, 2087500), (700000, 2087500),
    (620000, 2087500)
};
var checkpoints = new[]
{
	(1995100, 1937500),
	(1839000, 1812500),	
	(1765000, 2072500),	
	(1855000, 2327500),	
	(1675000, 2312500),	
	(1540000, 1877500),	
	(1780000, 1607500),	
	(1630000, 1187500),	
	(1210000, 992500), 	
	(1180000, 1262500),	
	(1105000, 1937500),	
	(780000, 2087500), 	
	(620000, 2087500)	
};

var startsteps = (int)(Math.Sqrt(Math.Pow(startx - poses[0].Item1, 2) + Math.Pow(startz - poses[0].Item2, 2)) / 100000) + 1;
poses = Enumerable.Range(1, startsteps).Select(i => ((startx * (startsteps - i) + poses[0].Item1 * i) / startsteps, (startz * (startsteps - i) + poses[0].Item2 * i) / startsteps))
            .Concat(poses).ToArray();
Console.WriteLine($"We will need to send {poses.Length} packets.");

var bigStopwatch = new Stopwatch();
var time = 0L;
foreach (var (x, z) in poses)
{
    var stp = Stopwatch.StartNew();
    var posPacket = new byte[] { 0, 0, 0x50 }.Concat(secret).Concat(BitConverter.GetBytes(++time * 100000))
            .Concat(BitConverter.GetBytes(x))
            .Concat(BitConverter.GetBytes(-20000))
            .Concat(BitConverter.GetBytes(z))
            .Concat(new byte[17])
            .ToArray();

    client.Send(posPacket, posPacket.Length);

    if ((x, z) == checkpoints[0]) bigStopwatch = Stopwatch.StartNew();
    if ((x, z) == poses.Last()) bigStopwatch.Stop();

    var delay = (x, z) == checkpoints[0] ? delay2 : delay1;

    while (stp.Elapsed < delay) ;

}

Console.WriteLine($"Estimated race time: {bigStopwatch.Elapsed.TotalMilliseconds}ms");
while (true)
{
    task = client.ReceiveAsync();
    if (!task.Wait(1000)) break;
    result = task.Result.Buffer;
    var code = result[0] ^ result[2];
    if (code == 0x54) {
        for (int i = 2; i < result.Length; i++)
        {
            result[i] ^= result[0];
            result[0] = (byte)((result[0] + result[1]) % 255);
        }
        Console.WriteLine($"Spawn of death: ({BitConverter.ToInt32(result, 4)}, {BitConverter.ToInt32(result, 12)})");
        break;
    }
    else if (code == 0x52) Console.WriteLine($"Checkpoint {((result[0] + result[1]) % 255) ^ result[3]}");
    else if (code == 0x43) Console.WriteLine("GotFlag");
    else if (code == 0x55) Console.WriteLine("Unlock");
    else Console.WriteLine($"Unknown code 0x{code:X2}.");
}
Console.WriteLine("The end");