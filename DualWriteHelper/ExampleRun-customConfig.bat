:: Copyright (c) Microsoft Corporation.
:: Licensed under the MIT License.
DWHelperCMD.exe -u "username@somewhere.com" -p "password" -e "yourenvironment.cloudax.dynamics.com" -c "yourCustomConfigFile.config"


::--runmode "deployment" --runmode "deployment" --runmode "deployInitialSync", Values: deployment, deployInitialSync, start, stop, pause export //overwrites the runmode in the config 
::-c "configFile.config" - optional runs the programm from a different config file
::-s in combination with runMode export "All" exports the current configuration of the environment, possible states: Running, Stopped, All
::--nosolutions  Skips applying the solution at the start 
::--mfasecret "secret" //overwrites / set the mfasecret in the config - usable in pipelines where the values comes from a key vault
::--useadowikiupload //overwrites / set  the useadowikiupload in the config and sets it to true - usable in pipelines where the values comes from a key vault, only works with mode deployment
::--adotoken "" //overwrites / set the wiki upload token - usable in pipelines where the values comes from a key vault
::-l "Information" //Loglevel, Values: Information, Debug, Error
pause