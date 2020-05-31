<Query Kind="Statements">
  <Namespace>System.Drawing</Namespace>
  <Namespace>System.Drawing.Imaging</Namespace>
  <Namespace>System.Net</Namespace>
  <Namespace>System.Net.Http</Namespace>
  <Namespace>System.Web</Namespace>
</Query>

var allweights = Regex.Matches(File.ReadAllText(@"captcha_model.json"), @"\[([0-9-., e]+)\]").Select(x => x.Groups[1].Value.Split(',').Select(double.Parse).ToArray()).ToArray();

using var client = new HttpClient() { BaseAddress = new Uri("http://hax1.allesctf.net:9200") };
while (true)
{
	var nextHtml = await (await client.GetAsync("/captcha/0")).Content.ReadAsStringAsync();
	
	var imgs = Regex.Matches(nextHtml, "base64,(.*)\"")
				.Select(x => x.Groups[1].Value)
				.Select(Convert.FromBase64String)
				.Select(x => (Bitmap)Image.FromStream(new MemoryStream(x)))
				.ToList();
	var preds = imgs.Select(Predict).ToList();
	
	for (int n = 0; n < 4; n++)
	{
		preds.Count().Dump("preds count");
		var content = new FormUrlEncodedContent(preds.Select((s, i) => new KeyValuePair<string, string>($"{i}", s)).ToArray());
		nextHtml = await (await client.PostAsync($"/captcha/{n}", content)).Content.ReadAsStringAsync();
		
		imgs = Regex.Matches(nextHtml, "base64,(.*)\"")
				.Select(x => x.Groups[1].Value)
				.Select(Convert.FromBase64String)
				.Select(x => (Bitmap)Image.FromStream(new MemoryStream(x)))
				.ToList();
			
		if (!imgs.Any()) { $"Failed round {n}".Dump(); break; }
		preds = imgs.Select(Predict).ToList();
		
		if (n == 3)
		{
			// we are the winners!
			imgs.Dump();
			preds.Dump();
			return;
		}
	}
}

///////////////////////////////////

char OCR(string str)
{
	if (str.Length != 400) throw new Exception();
	var input = str.Select(x => x - '0' - 0.5).ToArray();
	var (w1, layer1) = (allweights[0..400], allweights[400].ToArray());
	var (w2, layer2) = (allweights[401..465], allweights[465].ToArray());
	var (w3, layer3) = (allweights[466..530], allweights[530].ToArray());
	
	for (int i = 0; i < 400; i++)
		for (int j = 0; j < 64; j++)
			layer1[j] += w1[i][j] * input[i];
	layer1 = layer1.Select(x => Math.Max(0, x)).ToArray();
	
	for (int i = 0; i < 64; i++)
		for (int j = 0; j < 64; j++)
			layer2[j] += w2[i][j] * layer1[i];
	layer2 = layer2.Select(x => Math.Max(0, x)).ToArray();
	
	for (int i = 0; i < 64; i++)
		for (int j = 0; j < 35; j++)
			layer3[j] += w3[i][j] * layer2[i];
	
	return "0123456789ABCDEFGHIJKLMNPQRSTUVWXYZ"[layer3.Select((x,i)=>(x,i)).Max().i];
}

unsafe string Predict(Bitmap bmp)
{
	var w = bmp.Width;
	var pixels = new byte[w * 30];
	var alphas = new double[w * 30];
	var rect = new Rectangle(0, 0, w, 30);
	
	{
		var data = bmp.LockBits(rect, ImageLockMode.ReadWrite, bmp.PixelFormat);
		byte* scan0 = (byte*)data.Scan0;
		for (int y = 0; y < 30; y++)
			for (int x = 0; x < rect.Width; x++)
			{
				pixels[y * w + x] = scan0[y * data.Stride + x * 4];
				alphas[y * w + x] = scan0[y * data.Stride + x * 4 + 3] / 255.0;
			}
		bmp.UnlockBits(data);
	}
	
	const double weightThreshold = 0.005;
	var weights = Enumerable.Range(0, rect.Width).Select(x => Enumerable.Range(0, 30).Average(y => Math.Pow(alphas[y * w + x], 2))).ToArray();
	var tmp = string.Concat(weights.Select(x => x < weightThreshold ? 0 : 1));
	var matches = Regex.Matches(tmp, "1{4,}").Select(x => (x.Index, x.Length)).ToList();
	var draw = new bool[rect.Width];
	
	for (int i = 0; i < matches.Count; i++)
	{
		var m = matches[i];
		if (m.Length < 21) continue;
		var j = Enumerable.Range(m.Index + 6, m.Length - 12).OrderBy(j => weights[j]).First();
		
		if (m.Length > 25)
		j = Enumerable.Range(m.Index + 10, m.Length - 20).OrderBy(j => weights[j]).First();
		
		matches.Insert(i + 1, (j, m.Index + m.Length - j));
		matches[i] = (m.Index, j - m.Index);
		draw[j] = true;
		i--;
	}
	
	var strs = matches.Select(match =>
	{
		var (index, width) = match;
		var grid = new byte[400];
		var foo = (from y in Enumerable.Range(0, 30)
				   from x in Enumerable.Range(index, width)
				   let c = alphas[y * rect.Width + x]
				   select (c, c * x, c * y)).ToList();
		var full = foo.Sum(x => x.Item1);
		var cx = (int)(foo.Sum(x => x.Item2) / full + 0.5);
		var cy = (int)(foo.Sum(x => x.Item3) / full + 0.5);
		
		for (int y = 0; y < 20; y++)
			for (int x = 0; x < 20; x++)
			{
				var (fx, fy) = (cx - 10 + x, cy - 10 + y);
				if (fx >= index && fx < index + width && fy >= 0 && fy < 30)
				grid[y * 20 + x] = (byte)(255 - pixels[fy * w + fx]);
			}
			
		return string.Concat(grid.Select(x => x > 128 ? 1 : 0)); // use 0-255 if necessary
	}).ToArray();
	
	var mypred = string.Concat(strs.Select(OCR));
	return mypred;
}