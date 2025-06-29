import os
import time
import argparse
import subprocess
from pathlib import Path

parser = argparse.ArgumentParser("dESIGNER nOTIFIER")
parser.add_argument("folder", help="Folder to check", default=".", nargs="?")
parser.add_argument("provider", help="pyqt6/pyside6", default="pyside6", nargs="?")
args = parser.parse_args()
if not args.folder:
    args.folder = "."
if not args.provider:
    args.provider = "pyside6"

print(f"Folder: {args.folder}")
print(f"Provider: {args.provider}")
print("Press a key to continue...")
input()
folder = args.folder
folder = Path(os.path.abspath(folder))

inputs = {}
while True:
    time.sleep(0.2)
    fns = [f for f in folder.rglob("*") if f.is_file()]
    fns = list(filter(lambda x: Path(x).suffix == ".ui", fns))
    fns = list(map(lambda x: os.path.relpath(x, os.getcwd()), fns))
    fns = list(map(lambda x: (x, os.path.getmtime(x)), fns))
    fns = list(filter(lambda x: x[0] not in inputs.keys() or x[1] > inputs[x[0]], fns))
    for fn, mtime in fns:
        output = Path(fn).stem[0].title() + Path(fn).stem[1:]
        output = os.path.join(Path(fn).parent, f"{output}.py")

        print(f"{fn}. Changes have been intercepted...")
        print(f"\tOutput is {output}")
        print("\tCompiling...")

        cmd = []
        if args.provider == "pyqt6":
            cmd = f"/usr/bin/pyuic6 {fn} -o {output}"
        elif args.provider == "pyside6":
            cmd = f"pyside6-uic {fn} -o {output}"

        subprocess.call(cmd, shell=True)

        if not Path(output).is_file():
            print("\tSomething went wrong")
            continue

        print(f"\tFormatting output...")
        cmd = ["/usr/bin/black", f"{output}", "--quiet"]
        subprocess.call(cmd)
        inputs[fn] = mtime
