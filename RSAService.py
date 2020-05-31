from sympy import isprime, discrete_log, mod_inverse
from pyasn1.codec.der.encoder import encode
from pyasn1.type.univ import Sequence, Integer
from base64 import encodebytes

c = int.from_bytes(b"Hello! Can you give me the flag, please? I would really appreciate it!", "big")
m = int.from_bytes(b"Quack! Quack!", "big")

p = 11 # just pick some arbitrary prime here and change it until something works

for i in range(1, 10000):
    q = i * 2**559 + 1
    if not isprime(q):
        continue
    try:
        n = p*q
        d = discrete_log(n, c, m)
        e = mod_inverse(d,(p-1)*(q-1))
        if pow(m,d,n)!=c: raise
        if pow(c,e,n)!=m: raise
    except:
        print(f'i={i} failed')
        continue

    # Success! Let's construct the PEM file
    print(f'i={i} succeeded')
    print(f'd={d}')
    seq = Sequence()
    for i,x in enumerate([0, n, e, d, p, q, d%(p-1), d%(q-1), mod_inverse(q,p)]):
        seq.setComponentByPosition(i, Integer(x))
    b64 = encodebytes(encode(seq)).decode('ascii')
    print(f'-----BEGIN RSA PRIVATE KEY-----\n{b64}-----END RSA PRIVATE KEY-----')
    break
