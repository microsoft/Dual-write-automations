# Dual-Write automation for deployment and initial setup

## Disclaimer / Support
Please note that this is not supported by Microsoft in anyway. 
It is provided AS IS and it can break anytime because changes made in the API beeing used. 

## What can this tool do? 

Mainly this tool is intended as a utility to help save time during Dual-write setup and mainance tasks.
It doesen't look the prettiest but it does the job.

This is what it can do:

-	Apply the latest map version based on authors or any author
-	Apply integration keys
-	Refresh tables
-	Stop/Start the maps before and after
-	Run's on multi-threading, means multiple maps are applied at the same time.
-	Uploads maps to ADO Wiki 
-	Start / Stop / Pause maps 
-	Export configurations in the correct order 
-	Run initial sync 
-	Parallel deployment to multiple target environments only using command line or multiple instances of the UI

Generally the tool has a UI and a Console application execution. Ultimately the UI will call the console application with arguments. 
This makes it possible to also run any of what you are running in the UI also in an Azure pipeline. 
Be aware every function runs based on the configuration file, e.g. stopping maps will only stop the maps which are specified in the config. 

## How to get started? 

Download the pre-compiled application here: https://github.com/microsoft/Dual-write-automations/releases/
or clone the repo and compile it on your machine. 

1. Setup an environment with Dual-Write and the maps how you need/ want it
2. Export the configuration with the tool
3. Apply on other environments based on the config with the tool. 

Please refer to the Wiki page where the steps are described in details.
--> https://github.com/microsoft/Dual-write-automations/wiki

## Contributing

This project welcomes contributions and suggestions.  Most contributions require you to agree to a
Contributor License Agreement (CLA) declaring that you have the right to, and actually do, grant us
the rights to use your contribution. For details, visit https://cla.opensource.microsoft.com.

When you submit a pull request, a CLA bot will automatically determine whether you need to provide
a CLA and decorate the PR appropriately (e.g., status check, comment). Simply follow the instructions
provided by the bot. You will only need to do this once across all repos using our CLA.

This project has adopted the [Microsoft Open Source Code of Conduct](https://opensource.microsoft.com/codeofconduct/).
For more information see the [Code of Conduct FAQ](https://opensource.microsoft.com/codeofconduct/faq/) or
contact [opencode@microsoft.com](mailto:opencode@microsoft.com) with any additional questions or comments.

## Trademarks

This project may contain trademarks or logos for projects, products, or services. Authorized use of Microsoft 
trademarks or logos is subject to and must follow 
[Microsoft's Trademark & Brand Guidelines](https://www.microsoft.com/en-us/legal/intellectualproperty/trademarks/usage/general).
Use of Microsoft trademarks or logos in modified versions of this project must not cause confusion or imply Microsoft sponsorship.
Any use of third-party trademarks or logos are subject to those third-party's policies.
