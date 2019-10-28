# This program demonstrates how to write to the DSKY

	bank	2
	inhint

	zl	zero
	ca	const12
next	ts	lvar
dloop	index	l
	ca	digit1
	write	8
	incr	l
	ccs	lvar
        tc      next
dolight	ca	lights2
	write	9
	ca	lights3
	write	11
loop	tc	loop

digit1	hex	0x4003
digit2	hex	0x3f3b
digit3	hex	0x31fe

digit4	hex	0x2fcf
digit5	hex	0x2379
digit6	hex	0x1879

digit7	hex	0x11fc
digit8	hex	0x0fb5

noun	hex	0x487e
verb	hex	0x5333
prog	hex	0x5b8f
lights  hex	0x63ff
lights2	hex	0x00ff
lights3	hex	0x0800
const12	dec	12

	ebank	0
lvar	erase



