#!/usr/bin/python

# vec3 -- Utilities for operating on vec3's, represented as 3-tuples of floats.

import math

__min = min
__max = max

def add(v1, v2):
    return ( v1[0]+v2[0], v1[1]+v2[1], v1[2]+v2[2] )

def mult(val, scale):
    return ( val[0] * scale[0], val[1] * scale[1], val[2] * scale[2] )

def scale(val, scale):
    return ( val[0] * scale, val[1] * scale, val[2] * scale )

def len2(v):
    return v[0]*v[0] + v[1]*v[1] + v[2]*v[2]

def len(v):
    return math.sqrt(len2(v))

def dist(v1, v2):
    return len((v1[0]-v2[0],v1[1]-v2[1],v1[2]-v2[2]))

def dist2(v1, v2):
    return len2((v1[0]-v2[0],v1[1]-v2[1],v1[2]-v2[2]))

def min(v1, v2):
    return (__min(v1[0],v2[0]), __min(v1[1],v2[1]), __min(v1[2],v2[2]))

def max(v1, v2):
    return (__max(v1[0],v2[0]), __max(v1[1],v2[1]), __max(v1[2],v2[2]))

def equals(v1, v2):
    if v1 == None and v2 == None:
        return True
    if v1 == None or v2 == None:
        return False

    return (v1[0] == v2[0] and
            v1[1] == v2[1] and
            v1[2] == v2[2])

def create_delta_equals(fudge):
    """
    Returns a comparison function which allows up to fudge distance
    between two vec3's and still considers them equal.
    """

    def delta_equals(v1, v2):
        if v1 == None and v2 == None:
            return True
        if v1 == None or v2 == None:
            return False

        return dist2(v1, v2) < fudge*fudge

    return delta_equals
