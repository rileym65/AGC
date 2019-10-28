# This program contains and demonstrates the useage of a number
# of subroutines for convering numbers and displaying them on
# the DSKY

dskyprt	=	8

	bank	2
	inhint
	ca	arg1
	tc	dspverb
	ca	arg2
	tc	dspnoun
	ca	arg3
	tc	dspprog
	dca	arg4
	tc	dspreg1
	dca	arg5
	tc	dspreg2
	dca	arg6
	tc	dspreg3
	tc	upddsky
loop	tc	loop

arg1	dec	96
arg2	dec	37
arg3	dec	48
arg4	2dec	12789
arg5	2dec	24680
arg6	2dec	54321

# subroutines follow

# Display 2 digit value in A on verb display
dspverb	qxch	ret1		# save return address
	tc	conv2		# convert to dsky form
	ad	verbp		# add verb register prefix
	ts	dskverb		# transfer to storage
dsnoun2	qxch	ret1		# recover return address
	return			# and return
verbp	hex	0x5000

# Display 2 digit value in A on noun display
dspnoun	qxch	ret1		# save return address
	tc	conv2		# convert to dsky form
	ad	nounp		# add verb register prefix
	ts	dsknoun		# transfer to storage
	tc	dsnoun2		# jump to finish
nounp	hex	0x4800

# Display 2 digit value in A on program display
dspprog	qxch	ret1		# save return address
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

# Display A,L in register 1
dspreg1	qxch	ret1		# save return address
	tc	conv5		# convert number
	ca	digit1		# get most significant digit
	ad	creg1		# add in control constant
	ts	dskreg1		# store it
	dca	digit2		# get digits 2 and 3
	tc	pack		# pack them for display
	ad	creg2		# and register constant
	ts	dskreg2		# write to storage
	dca	digit4		# get digits 4 and 5
	tc	pack		# pack them for display
	ad	creg3		# and register constant
	ts	dskreg3		# write to storage
	qxch	ret1		# recover return address
	return			# and return

# Display A,L in register 2
dspreg2	qxch	ret1		# save return address
	tc	conv5		# convert number
	dca	digit1		# get digits 1 and 2
	tc	pack		# pack them
	ad	creg4		# add in dsky code
	ts	dskreg4		# save it
	dca	digit3		# get digits 3 and 4
	tc	pack		# pack them
	ad	creg5		# add in dsky code
	ts	dskreg5		# save it
	ca	dskreg6		# get current register
	mask	c31		# keep register 3 value
	xch	l		# move it to l
	ca	digit5		# get final digit
	tc	pack		# pack them
	ad	creg6		# add in dsky code
	ts	dskreg6		# save it
	qxch	ret1		# recover return address
	return			# and return

# Display A,L in register 3
dspreg3	qxch	ret1		# save return address
	tc	conv5		# convert number
	ca	dskreg6		# get current register 6
	mask	c3e0		# keep only number from register 2
	ad	digit1		# add in digit 1
	ad	creg6		# and dsky code
	ts	dskreg6		# write back to memory
	dca	digit2		# Get digits 2 and 3
	tc	pack		# pack for display
	ad	creg7		# add in dsky code
	ts	dskreg7		# transfer to memory
	dca	digit4		# get digits 4 and 5
	tc	pack		# pack for display
	ad	creg8		# add in dsky code
	ts	dskreg8		# write to memory
	qxch	ret1		# recover return address
	return			# and return

# pack A,L into A for display
pack	ts	cyl		# shift high number 1
	ca	cyl		# 2
	ca	cyl		# 3
	ca	cyl		# 4
	ca	cyl		# 5
	ca	cyl		# retrieve shifted number
	ad	l               # add in L
	return			# and return

# Convert 5 digit number in A,L to dsky display form
conv5	qxch	retc5		# save return address
	dxch	tmp3		# Save number while setting up
	ca	c10		# Needed for the divisions
	ts	tmp1		# store here
	ca	c4		# 5 digits to process
conv5lp	ts	tmp2		# store here
	dxch	tmp3		# recover number
	dv	tmp1		# peform division
	xch	l		# Need value in L
	index	a		# Convert number to dsky form
	ca	convert		# Complete the conversion
	index	tmp2		# modify storage location
	ts	digit1		# Store into memory
	ca	zero		# zero A
	dxch	tmp3		# save current number
	ccs	tmp2		# decrement count
	tc	conv5lp		# jump if more to go
	qxch	retc5		# recover return address
	return			# and return

upddsky	qxch	ret1		# save return address
	ca	c10		# 11 registers to output
updlp	ts	tmp1		# loop counter
	index	tmp1		# offset
	ca	dskprog		# read from display table
	write	dskyprt		# output to display
	ccs	tmp1		# check loop
	tc	updlp		# loop back if not done
	qxch	ret1		# recover return address
	return			# and return

# This is the table for digit to dsky display codes
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

c4	dec	4
c10	dec	10
c31	hex	0x1f
c3e0	hex	0x3e0
creg1	hex	0x4000
creg2	hex	0x3800
creg3	hex	0x3000
creg4	hex	0x2800
creg5	hex	0x2000
creg6	hex	0x1800
creg7	hex	0x1000
creg8	hex	0x0800

	ebank	0
tmp1	erase
tmp2	erase
tmp3	erase	2
ret1	erase
retc5	erase
digit1	erase
digit2	erase
digit3	erase
digit4	erase
digit5	erase
dskprog	erase
dskverb	erase
dsknoun	erase
dskreg1	erase
dskreg2	erase
dskreg3	erase
dskreg4	erase
dskreg5	erase
dskreg6	erase
dskreg7	erase
dskreg8	erase



