# zip2
**v2.1.2**

## Example:

### List
```
zip2 ..\backup.zip
zip2 ..\backup2.rar
zip2 ..\test3.epub -Zv
```

### Extract
```
zip2 ..\backup.zip -x
zip2 ..\backup2.rar -x
zip2 ..\test3.epub -Zx
```

### Create Zip
Backup files in dir ```src```, which timestamp is within 2 hours, into a new zip file.
```
dir2 src -bsk --within 2hour | zip2 -cf ..\new.zip -T -
```
* [Link to tool ```dir2```](https://www.nuget.org/packages/dir2)

## On-line Help
List ```zip2 -t?```

Extract ```zip2 -x?```

Create ```zip2 -c?```

## Syntax:

### Zip filename is the first parameter
```
zip2 -c NEW-ZIP-FILENAME [OPTION ..] [FILE ..]

zip2 FILENAME.zip -tv [OPTION ..] [WILD ..]
zip2 FILENAME.rar -tv [OPTION ..] [WILD ..]

zip2 FILENAME.zip -x  [OPTION ..] [WILD ..]
zip2 FILENAME.rar -x  [OPTION ..] [WILD ..]
```

### Specify Zip filename by option ```--file```
```
zip2 [FILE ..] -cf NEW-ZIP-FILENAME [OPTION ..] [FILE ..]

zip2 [WILD ..] -tvf FILENAME.zip [OPTION ..] [WILD ..]
zip2 [WILD ..] -tvf FILENAME.rar [OPTION ..] [WILD ..]

zip2 [WILD ..] -xf FILENAME.zip [OPTION ..] [WILD ..]
zip2 [WILD ..] -xf FILENAME.rar [OPTION ..] [WILD ..]
```

[Link to Feature Changes](https://github.com/ck-yung/zip2/blob/main/History.md)

## Credit:
* SharpZipLib v1.4.2
* SharpCompress v0.34.2

2021, 2024 (c) Yung, Chun Kau

yung.chun.kau@gmail.com
