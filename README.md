# zip2
**v2.0.0**

## Example:
```
zip2 -xf ..\backup.zip -O restore-dir
```

## Syntax:
```
zip2 -cf NEW-ZIP-FILENAME [OPTION ..] [FILE ..]
zip2 -tf ZIP-FILENAME [OPTION ..] [FILE ..]
zip2 -xf ZIP-FILENAME [OPTION ..] [FILE ..]
```

## Specified Example:
```
dir2 srcDir -bsp --within 2hour | zip2 -cf ..\new.zip -T -
```

## Major Change:
* No password support
* Two-way Async IO in read/write files.

## Credit:
* SharpZipLib v1.4.1

2021, 2023 (c) Yung, Chun Kau

yung.chun.kau@gmail.com
