dskyprt	=	8

	bank	2
	inhint
	ca	arg1
	tc	dspverb
	ca	arg2
	tc	dspnoun
	ca	arg3
	tc	dspprog
loop	tc	loop

arg1	dec	96
arg2	dec	37
arg3	dec	48

# Display 2 digit value in A on verb display
dspverb	xch	q		# get return address
	ts	ret1		# and save it
	xch	q		# recover number
	tc	conv2		# convert to dsky form
	ad	verbp		# add verb register prefix
	ts	dskverb		# transfer to storage
dsnoun2	write	dskyprt		# and write to display
	ca	ret1		# recover return address
	xch	q		# put it back in Q
	return			# and return
verbp	hex	0x5000

# Display 2 digit value in A on noun display
dspnoun	xch	q		# get return address
	ts	ret1		# and save it
	xch	q		# recover number
	tc	conv2		# convert to dsky form
	ad	nounp		# add verb register prefix
	ts	dsknoun		# transfer to storage
	tc	dsnoun2		# jump to finish
nounp	hex	0x4800

# Display 2 digit value in A on program display
dspprog	xch	q		# get return address
	ts	ret1		# and save it
	xch	q		# recover number
	tc	conv2		# convert to dsky form
	ad	progp		# add verb register prefix
	ts	dskprog		# transfer to storage
	tc	dsnoun2		# jump to finish
progp	hex	0x5800

# Convert 2 digit number in A to dsky display form
conv2	ts	l		# Transfer number to L
	ca	c10		# need to divide by 10
	ts	tmp1		# Transfer for division
	ca	zero		# ready for division
	dv	tmp1		# A=high, L=low
	index	a		# convert to dsky format
	ca	convert
	ts	cyl		# shift by 1
	ca	cyl		# by 2
	ca	cyl		# by 3
	ca	cyl		# by 4
	ca	cyl		# by 5
	ca	cyl		# retrieve fully shifted value
	ts	tmp2		# transfer to storage
	ca	l		# get low word
	index	a		# convert to dsky format
	ca	convert
	ad	tmp2		# merge in with high value
	return			# return answer in A to caller

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
tmp1	erase
tmp2	erase
ret1	erase
dskprog	erase
dskverb	erase
dsknoun	erase

