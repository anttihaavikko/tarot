#!/usr/bin/env python3
import random
lines = open('cliches.txt').read().splitlines()
print('--------------')
for x in range(10):
	myline = random.choice(lines)
	print(myline)
