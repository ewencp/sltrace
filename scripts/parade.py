#!/usr/bin/python
#
# parade.py - instantiates a collection of sltrace.exe bots to
# coordinate collection of trace data from Second Life across a large
# number of simulators. The name refers to a parade (herd) of
# elephants, which are known for their memory.
#
# There are two parameters to parade:
#
#  --bots -- specifies a JSON configuration file containing a list of
#            bot accounts, each account a dictionary containing
#            "first", "last", and "password" keys.
#  --sims -- specifies a JSON configuration file containing a list of
#            sims to connect to.  It should contain a list of
#            secondlife:// URL strings.
#
# These parameters are separated so that the bots file can be reused
# easily, while the sims file might be used for only one collection.
#
# Any other parameters are passed to the invoked bots, e.g. adding
# --duration=1h will cause --duration=1h to be appended to every
# sltrace.exe command line, causing all bots to colelct 1 hour
# traces. In order to allow per-instance customization, each bot
# instance will be assigned in index and any instance of the substring
# "bot" in the additional arguments will be replaced with that index.
# For instance, specifying --tracer-args=--out=mytrace.bot.json would
# generate log files mytrace.1.json, mytrace.2.json, etc.

import sys
import subprocess

try:
    import simplejson as json
except:
    import json


def _load_bots(bots_file):
    # FIMXE validate
    return json.load(open(bots_file))

def _load_sims(sims_file):
    # FIMXE validate
    return json.load(open(sims_file))


def main():
    bots_file = None
    sims_file = None
    pass_args = []

    for arg in sys.argv[1:]:
        if arg.startswith('--bots='):
            bots_file = arg.split('=', 1)[1]
        elif arg.startswith('--sims='):
            sims_file = arg.split('=', 1)[1]
        else:
            pass_args.append(arg)

    if bots_file == None:
        print "Must specify bots file using --bots"
        return -1
    if sims_file == None:
        print "Must specify sims file using --sims"
        return -1

    bots = _load_bots(bots_file)
    if bots == None:
        print "Invalid bots file."
        return -1
    sims = _load_sims(sims_file)
    if sims == None:
        print "Invalid sims file."
        return -1

    if len(bots) < len(sims):
        print "Bots file doesn't contain enough bots for number of sims specified."
        return -1

    processes = []
    for idx in xrange(len(sims)):
        bot = bots[idx]
        sim = sims[idx]

        command = ["./bin/sltrace.exe"]
        command.append("--first=" + bot['first'])
        command.append("--last=" + bot['last'])
        command.append("--password=" + bot['password'])
        command.append("--url=" + sim)
        for pass_arg in pass_args:
            command.append(pass_arg.replace('bot', str(idx)))

        proc = subprocess.Popen(command)
        processes.append(proc)

    for proc in processes:
        proc.wait()

    return 0

if __name__ == "__main__":
    sys.exit(main())
