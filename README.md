# Instance Painter

Tool to paint prefabs that will be rendered using GPU instancing.

## Installation

#### Install Unity Package Manager 

Using Package Manager is now the prefferred method, all releases should be updated immediately.

Add Scoped Registry into Package manager using Project Settings => Package Manager as below:  
Name:
```
BinaryEgo Registry
```  
URL:
```
http://package.binaryego.com:4873
```
Scopes:
```
com.shtif
```


> #### WARNING
> If you have com.teamsirenix.odinserializer scope defined in your registries already just use com.shtif to avoid scope duplication.
> Also if you are using OdinSerializer in your project already you don't need to install it Dash should link it using the Assembly Definition reference. If Dash can't find the OdinSerialializer you can also modify the Dash Assembly Definition files in Dash folders Runtime and Editor to correctly include your OdinSerializer reference.
