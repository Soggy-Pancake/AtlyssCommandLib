import os, json, argparse, re

os.chdir(os.path.dirname(os.path.abspath(__file__)))

versionNum = "null"

def main():
    with open("ThunderStore/manifest.json", 'r+') as original:
        test = json.load(original)
        test['version_number'] = versionNum
        original.seek(0)
        original.truncate()
        json.dump(test, original, indent=4)

    with open("src/Info.cs", 'r+') as info:
        lines = info.readlines()

        for i in range(len(lines)):
            line = lines[i]
            if " string VERSION = " in line:
                string = line[:line.find('=') + 2]
                string += f'"{versionNum}";\n'
                lines[i] = string
                break

        info.seek(0)
        info.truncate(0)
        info.writelines(lines)


if __name__ == "__main__":
    parser = argparse.ArgumentParser(description="Version number aligner")
    parser.add_argument("VersionNum", help="Semantic version number")

    args = parser.parse_args()

    #print(args.VersionNum)
    if(re.findall(r"\d+\.\d+\.\d+", args.VersionNum)):
        versionNum = args.VersionNum
        main()
    else:
        print("Invalid version number!")