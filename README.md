# zip2
**v2.0.2**

## Example:
```
zip2 -xf ..\backup.zip -O restore-dir
```

## Specified Example:
Backup files in dir ```srcDir```, which timestamp is within 2 hours, into a new zip file.
```
dir2 srcDir -bsk --within 2hour | zip2 -cf ..\new.zip -T -
```

## Syntax:
```
zip2 -cf NEW-ZIP-FILENAME [OPTION ..] [FILE ..]

zip2 -f FILENAME.zip -tv [OPTION ..] [FILE ..]
zip2 -f FILENAME.rar -tv [OPTION ..] [FILE ..]

zip2 -f FILENAME.zip -x  [OPTION ..] [FILE ..]
zip2 -f FILENAME.rar -x  [OPTION ..] [FILE ..]
```

[Link to Feature Changes](https://github.com/ck-yung/zip2/blob/main/History.txt)

## Credit:
* SharpZipLib v1.4.1
* SharpCompress v0.32.2

### Remark:
You can install ```dir2``` by ```dotnet tool install dir2 -g```

2021, 2023 (c) Yung, Chun Kau

yung.chun.kau@gmail.com
