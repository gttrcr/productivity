# GitSync

GitSync is a file manager based on GitHub. It is written in c# and compiled under dotnet. Hence,
```
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