<p align="center">
<a href="https://neo.org/">
      <img
      src="https://neo3.azureedge.net/images/logo%20files-dark.svg"
      width="250px" alt="neo-logo">
  </a>
</p>

<p align="center">      
  <a href="https://travis-ci.org/neo-project/neo-modules">
    <img src="https://travis-ci.org/neo-project/neo-modules.svg?branch=master" alt="Current TravisCI build status.">
  </a>
  <a href="https://github.com/neo-project/neo-modules/blob/master/LICENSE">
    <img src="https://img.shields.io/badge/license-MIT-blue.svg" alt="License">
  </a>
  <a href="https://github.com/neo-project/neo-modules/releases">
    <img src="https://badge.fury.io/gh/neo-project%2Fneo-modules.svg" alt="Current neo-modules version.">
  </a>    
</p>

## What is it

A set of plugins/modules that can be used inside the NEO core library is available in this repository. You can refer to [the official documentation](https://docs.neo.org/docs/en-us/node/cli/setup.html) for the more detailed usage guide. 

In addition, a C# SDK module is included for developers to call RPC methods with ease.

## Using Plugins
Plugins can be used to increase functionality, as well as providing policies definitions of the network.
One common example is to add the ApplicationLogs plugin in order to enable your node to create log files.

To configure a plugin, you can directly download the desired plugin from the [Releases page](https://github.com/neo-project/neo-modules/releases).

Alternatively, you can compile from source code by following the below steps:
- Clone this repository;
- Open it in Visual Studio, select the plugin you want to enable and select `publish` \(compile it using Release configuration\)
- Create the Plugins folder in neo-cli / neo-gui (where the binary file is located, such as `/neo-cli/bin/Release/netcoreapp3.0/Plugins`)
- Copy the .dll and the folder with the configuration files into the `Plugins` folder.
  - Remarkably, you should put the dependency of the plugin in the `Plugins` folder as well. For example, since the `RpcServer` has the package reference on the `Microsoft.AspNetCore.ResponseCompression`, so the corresponding dll file should be put together with the plugin.

The resulting folder structure is going to be like this:

```sh
./neo-cli.dll
./Plugins/ApplicationLogs.dll
./Plugins/ApplicationsLogs/config.json
```

## Plugins/Modules

### StatesDumper
Exports neo-cli status data \(useful for debugging\), such as storage modifications block by block.

### SystemLog
Enable neo-cli Logging with timestamps by showing messages with different levels (shown with different colors) \(useful for debugging\).

### ApplicationLogs
Add this plugin to your application if need to access the log files. This can be useful to handle notifications, but remember that this also largely increases the space used by the application. `RpcServer` is also needed for this plugin. You can find more details [here](https://docs.neo.org/docs/en-us/reference/rpc/latest-version/api/getapplicationlog.html).

### Storage Engine
There, currently, two option for users for the storage engine.

### RPC NEP5 Tracker
Plugin that enables NEP5 tracking using LevelDB.

#### LevelDBStore
If there is no further modification of the configuration file of the neo-node, it is the default storage engine in the NEO system. In this case, you should paste the `LevelDBStore` in the Plugins before launching the node.

#### RocksDBStore
You can also use `RocksDBStore` in the NEO system by modifying the default storage engine section in the configuration file.

### C# SDK

#### RpcClient
The RpcClient Plugin is an individual SDK which is used to call NEO RPC methods for development using.
In order to use the module for interacting with NEP5 functionalities `RPC NEP5 Tracker` Plugin will be needed.
