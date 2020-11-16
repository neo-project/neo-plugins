It is a plugin to create performance metrics used to benchmark NEO3. You can follow the instructions on [neo-modules](https://github.com/neo-project/neo-modules/blob/master/README.md) to install the plugin.

>Note: 
> 1. Install `RpcClient` module to use the command `rpc time`. 
> 2. It is required to start consensus to use the command `confirmation time` and `payload time`.
 
After installing the plugin, you can type the command `help PerformanceMonitor` to get the full list of available commands.

The available metrics cover:
- Block properties:
    - `block time <index/hash>`: block activated time (s) 
    - `block avgtime [1-10000]`: average activated time of the latest blocks (s), 1000 by default
    - `block timesincelast`: the time passed since the last block (s)
    - `block sync`: the delay time in the synchronization of the blocks (s)
- Consensus Algorithm:
    - `commit time`: the time to commit in the network (ms)
    - `confirmation time`: the time to confirm the block (ms)
    - `payload time`: the time to receive a payload (ms)
- Network Protocol:
    - `connected`: the number of nodes connected to the local node
    - `ping [ipaddress]`: If `ipaddress` specified, send a ping message to the specified node; otherwise, send a ping message to each peer connected 
    - `rpc time <url>`: the time to receive the response of a rpc request (ms). eg: rpc time http://seed4.ngd.network:10332
- Transaction properties:
    - `tx size <hash>`: the size of the transaction (bytes)
    - `tx avgsize [1-10000]`: the average size of the latest transactions (bytes), 1000 by default
- Others:
    - `check state`: the amount of memory allocated for the current process (MB), total CPU usage and each thread CPU usage information (%)
