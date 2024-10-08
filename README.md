# GitSync

GitSync is a file manager based on GitHub. It is written in c# and compiled under dotnet. Hence,

```bash
sudo apt install dotnet7 -y
dotnet run orgs.json
```

where ```orgs.json``` is just a configuration file for GitSync. The structure of the ```orgs.json``` is the following:

```json
{
    "Organizations": [
        {
            "Organization": "name_of_organization_1",
            "Repos": [
                "repo_1",
                "repo_2",
                "-repo_3",
            ]
        },
        {
            "Organization": "name_of_organization_2",
            "Repos": [
                "repo_1",
                "repo_2",
            ]
        },
        {
            "Organization": "name_of_organization_3",
            "Repos": [
                "*"
            ]
        },
    ],
    "Path": "root/of/your/organizations"
}
```

The value ```"*"``` in ```Repos``` key means _every repo_ is the corresponding organization.

## Setup alias using the source code

```bash
cat >>~/.bashrc <<EOL

gitsync() {
    if [ -z "$1" ]
    then
        dotnet run --project path_to_folder/GitSync/ path_to_folder/GitSync/orgs.json
    else
        dotnet run --project path_to_folder/GitSync/ path_to_folder/GitSync/orgs.json $1
    fi
}
EOL
```

Consider the command for future improvements

```bash
git submodule update --remote --recursive --merge --init --force
git submodule foreach git pull
```
