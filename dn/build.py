import argparse
import PyInstaller.__main__


def main():
    parser = argparse.ArgumentParser(
        description="Build a Python script into an executable using PyInstaller."
    )
    parser.add_argument("script", help="The Python script to package")
    parser.add_argument("--name", default="my_app")
    parser.add_argument("--build-path", default="./build")
    parser.add_argument("--dist-path", default="./dist")
    parser.add_argument("--add-data", default=[], action="append")
    parser.add_argument("--windowed", action="store_true")
    parser.add_argument("--onefile", action="store_true")
    parser.add_argument("--onedir", action="store_true")
    parser.add_argument("--icon", default=None)

    args = parser.parse_args()
    if args.onedir and args.onefile:
        print("Cannot --onedir and --onefile at the same time")
        return

    pyinstaller_args = [
        "--clean",
        "--noconfirm",
        "--log-level=TRACE",
        "--optimize=2",
        f"--name={args.name}",
        f"--workpath={args.build_path}",
        f"--specpath={args.build_path}",
        f"--distpath={args.dist_path}",
    ]

    if args.icon:
        pyinstaller_args.append(f"--icon={args.icon}")

    for data in args.add_data:
        pyinstaller_args.append(f"--add-data={data}")

    if args.onefile:
        pyinstaller_args.append("--onefile")

    if args.onedir:
        pyinstaller_args.append("--onedir")

    if args.windowed:
        pyinstaller_args.append("--windowed")

    pyinstaller_args.append(args.script)

    PyInstaller.__main__.run(pyinstaller_args)


if __name__ == "__main__":
    main()
