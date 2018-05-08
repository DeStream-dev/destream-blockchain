## DeStream DNS Crawler 
The DeStream DNS Crawler provides a list of DeStream full nodes that have recently been active via a custom DNS server.

### Prerequisites

To install and run the DNS Server, you need
* [.NET Core 2.0](https://www.microsoft.com/net/download/core)
* [Git](https://git-scm.com/)

## Build instructions

### Get the repository and its dependencies

```
git clone https://github.com/DeStream-dev/destream-blockchain.git  
cd DeStreamBitcoinFullNode
git submodule update --init --recursive
```

### Build and run the code
With this node, you can run the DNS Server in isolation or as a DeStream node with DNS functionality:

1. To run a <b>DeStream</b> node <b>only</b> on <b>MainNet</b>, do
```
cd DeStream.DeStreamDnsD
dotnet run -dnslistenport=5399 -dnshostname=dns.destreamplatform.com -dnsnameserver=ns1.dns.destreamplatform.com -dnsmailbox=admin@destreamplatform.com
```  

2. To run a <b>DeStream</b> node and <b>full node</b> on <b>MainNet</b>, do
```
cd DeStream.DeStreamDnsD
dotnet run -dnsfullnode -dnslistenport=5399 -dnshostname=dns.destreamplatform.com -dnsnameserver=ns1.dns.destreamplatform.com -dnsmailbox=admin@destreamplatform.com
```  

3. To run a <b>DeStream</b> node <b>only</b> on <b>TestNet</b>, do
```
cd DeStream.DeStreamDnsD
dotnet run -testnet -dnslistenport=5399 -dnshostname=dns.destreamplatform.com -dnsnameserver=ns1.dns.destreamplatform.com -dnsmailbox=admin@destreamplatform.com
```  

4. To run a <b>DeStream</b> node and <b>full node</b> on <b>TestNet</b>, do
```
cd DeStream.DeStreamDnsD
dotnet run -testnet -dnsfullnode -dnslistenport=5399 -dnshostname=dns.destreamplatform.com -dnsnameserver=ns1.dns.destreamplatform.com -dnsmailbox=admin@destreamplatform.com
```  

### Command-line arguments

| Argument      | Description                                                                          |
| ------------- | ------------------------------------------------------------------------------------ |
| dnslistenport | The port the DeStream DNS Server will listen on                                       |
| dnshostname   | The host name for DeStream DNS Server                                                 |
| dnsnameserver | The nameserver host name used as the authoritative domain for the DeStream DNS Server |
| dnsmailbox    | The e-mail address used as the administrative point of contact for the domain        |

### NS Record

Given the following settings for the DeStream DNS Server:

| Argument      | Value                             |
| ------------- | --------------------------------- |
| dnslistenport | 53                                |
| dnshostname   | destreamdns.destreamplatform.com    |
| dnsnameserver | ns.destreamdns.destreamplatform.com |

You should have NS and A record in your ISP DNS records for your DNS host domain:

| Type     | Hostname                          | Data                              |
| -------- | --------------------------------- | --------------------------------- |
| NS       | destreamdns.destreamplatform.com    | ns.destreamdns.destreamplatform.com |
| A        | ns.destreamdns.destreamplatform.com | 192.168.1.2                       |

To verify the DeStream DNS Server is running with these settings run:

```
dig +qr -p 53 destreamdns.destreamplatform.com
```  
or
```
nslookup destreamdns.destreamplatform.com
```
