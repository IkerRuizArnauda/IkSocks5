# IkSocks5
Clean and simple socks5 server.

Target .NET Framework 4.7.2

![](https://raw.githubusercontent.com/IkerRuizArnauda/IkSocks5/master/IkSocks5.PNG?token=AAOQJLC2SJGCEJPTT6SGWMC7HU6GC)

# Features
Type | State
:------------ | :-------------
IPv4 | :heavy_check_mark:
IPv6 | :heavy_check_mark:
MultiThread | :heavy_check_mark:
Authentication | :heavy_check_mark:
CONNECT | :heavy_check_mark:
BIND | :x:
UDP  | :x:

# Authentication support
Type | State
:------------ | :-------------
NoAuthentication | :heavy_check_mark:
Username/Password | :heavy_check_mark:
GSSAPI | :x:
OTHERS | :x:

# Configuration
Type | File | Format
:------------ | :------------- | :-------------
Account | Accounts.ini | username=password
Server  | Configuration.ini | Ip,Port,AuthMethod
