# This program demonstrates some interrupt routines for reading
# numbers from the DSKY and convering a digit for display on 
# the DSKY

dsky	=	8
keyb	=	13

	bank	2
	tc	start

	setloc	0x804
t6rupt	resume

	setloc	0x808
t5rupt	resume

	setloc	0x80c
t3rupt	qxch	qrupt
	dxch	arupt
	tc	dspdsky

	setloc	0x810
t4rupt	resume

	setloc	0x814
keyrupt	qxch	qrupt		# Save Q before transfer
	dxch	arupt
	tc	keydrvr		# jump to keyboard reader

start	ca	number		# Get number to convert
	tc	cnvdsky		# Call conversion routine
	ad	reg1		# Add in code for register 1
	ts	dskyr1		# Write to DSKY port
	ca	timeval
	ts	time3
loop	tc	loop

# Convert value in A to DSKY value for A
cnvdsky	index	a
	ca	convert
	return
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

# Read key from DSKY and display
keydrvr	read	keyb		# Read key from IO channel
	tc	cnvdsky		# convert for display
	ad	reg1		# Add prefix code for DSKY
	ts	dskyr1		# put into DSKY storage area
intret	dxch	arupt
	qxch	qrupt
	resume			# and exit routine

dspdsky	ca	dskyr1		# get DSKY register 1
	write	dsky		# send to DSKY
	ca	timeval		# get timer reset value
	ts	time3		# Reset timer
	tc	intret

number	dec	7
timeval	dec	16373
reg1	hex	0x4000
	
	ebank	0
answer	erase
dskyr1	erase



