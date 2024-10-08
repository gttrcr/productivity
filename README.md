# GitSync

GitSync is a file manager based on GitHub. It is written in c# and compiled under dotnet. Hence,

```bash
sudo apt install dotnet7 -y
dotnet run orgs.json
```

where ```orgs.json``` is just a configuration file for GitSync. The structure of the ```orgs.json``` is the following:

```json
{
    "Repo": [
        {
            "Organization": "name_of_organization_1",
            "Repo": [
                "repo_1",
                "repo_2",
                "-repo_3",
            ]
        },
        {
            "Organization": "name_of_organization_2",
            "Repo": [
                "repo_1",
                "repo_2",
            ]
        },
        {
            "Organization": "name_of_organization_3",
            "Repo": [
                "*"
            ]
        },
    ],
    "Path": "root/of/your/organizations"
}
```

The value ```"*"``` in ```Repo``` key means _every repo_ is the corresponding organization.

## Setup alias using the source code

```bash
cat >>~/.bashrc <<EOL

gitsync() {
    if [ -z "$1" ]
    then
        dotnet run --project ~/gttrcr/GitSync/ ~/gttrcr/GitSync/orgs.json
    else
        dotnet run --project ~/gttrcr/GitSync/ ~/gttrcr/GitSync/orgs.json $1
    fi
}
EOL
```

Consider the command for future improvements

```bash
git submodule update --remote --recursive --merge --init --force
git submodule foreach git pull
```
