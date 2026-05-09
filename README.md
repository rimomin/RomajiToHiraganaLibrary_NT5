# RomajiToHiraganaLibrary
与えられた文字列に含まれるローマ字をひらがなに置換します

## 使い方
```
string hoge = "shigobu";
hoge = RomajiToHiragana.Convert(hoge);
Console.WriteLine(hoge);    //出力:しごぶ
```

## RomajiToHiraganaLibrary_NT5
このリポジトリはshigobuさんのRomajiToHiraganaLibraryをWindows NT 5.xで使用可能とすることを目的としています。

第一段階として、Windows XPで実行可能な最終バージョンであるVisual Studio 2010と.NET Framework 4.0の組み合わせコンパイルできるように変更しています。

将来的には、Visual C# 2005と.NET Framework 2.0 の組み合わせコンパイルできるようにすることでWindows 2000でも使用可能とすることを目標としています。