#!/usr/bin/python

# vec3 -- Utilities for operating on vec3's, represented as 3-tuples of floats.

import math

__min = min
__max = max

def mult(val, scale):
    return ( val[0] * scale[0], val[1] * scale[1], val[2] * scale[2] )

def len(v):
    return math.sqrt(v[0]*v[0] + v[1]*v[1] + v[2]*v[2])

def dist(v1, v2):
    return len((v1[0]-v2[0],v1[1]-v2[1],v1[2]-v2[2]))

def min(v1, v2):
    return (__min(v1[0],v2[0]), __min(v1[1],v2[1]), __min(v1[2],v2[2]))

def max(v1, v2):
    return (__max(v1[0],v2[0]), __max(v1[1],v2[1]), __max(v1[2],v2[2]))
