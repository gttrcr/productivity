import os
import time
import argparse
import subprocess
from pathlib import Path

parser = argparse.ArgumentParser("dESIGNER nOTIFIER")
parser.add_argument("folder", help="Folder to check", type=str)
args = parser.parse_args()
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
        output = os.path.join(Path(fn).parent, f"UI{output}.py")

        print(f"{fn}. Changes have been intercepted...")
        print(f"\tOutput is {output}")
        print("\tCompiling...")
        cmd = ["/usr/bin/pyuic6", f"{fn}", f"-o{output}"]
        subprocess.call(cmd)

        if not Path(output).is_file():
            print("\tSomething went wrong")
            continue

        print(f"\tFormatting output...")
        cmd = ["/usr/bin/black", f"{output}", "--quiet"]
        subprocess.call(cmd)
        inputs[fn] = mtime
