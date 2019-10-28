dskyprt	=	8

	bank	2
	inhint
	ca	arg1
	tc	dspverb
loop	tc	loop

arg1	dec	96

dspverb	ts	l		# Transfer number to L
	ca	c10		# need to divide by 10
	ts	earg2		# Transfer for division
	ca	zero		# ready for division
	dv	earg2		# A=high, L=low
	index	a		# convert to dsky format
	ca	convert
	ts	cyl		# shift by 1
	ca	cyl		# by 2
	ca	cyl		# by 3
	ca	cyl		# by 4
	ca	cyl		# by 5
	ca	cyl		# retrieve fully shifted value
	ad	verbp		# add verb register prefix
	ts	dskverb		# transfer to storage
	ca	l		# get low word
	index	a		# convert to dsky format
	ca	convert
	ads	dskverb		# merge in with high value
	write	dskyprt		# write to dsky
	return
verbp	hex	0x5000

convert	dec	21		# 0
	dec	3		# 1
	dec	25		# 2
	dec	27		# 3
	dec	15		# 4
	dec	30		# 5
	dec	28		# 6
	dec	19		# 7
	dec	29		# 8
	dec	31		# 9

c10	dec	10

	ebank	0
earg2	erase
dskverb	erase



