# ResourceMgrForRasPi
## 概要
ラズベリーパイ上で動く、簡易的なリソースモニターです。<br>
![image](https://github.com/user-attachments/assets/e6aa3b11-e8d3-4c8b-ab9f-ca39d05ed9a6) <br>
...今思ったのですがこれ"マネージャー"ではないきｇ(((((<br>

## ファイル説明
ResourceMgrForRasPiフォルダー内のファイルは本体の、<br>
RMFRInstallerフォルダー内のファイルはインストーラーの<br>
ソース(一部)が入っています<br>

## 使用方法
### Setup
0. 必須アプリを導入します(下項目参照)
1. リリースから最新のバージョンをダウンロードします
2. RaspberryPi上で、インストーラーを**sudoで**実行します
3. インストールを画面の指示に従って進めます
4. インストール終了後、(/usr/bin内に保存した場合は)
   `rmfr`で実行できます

### 必須アプリ
 - .NET8 Runtime
 - sysstat
 - 
導入方法は各自で検索してください。

### Use
 - Rキーでリフレッシュ
 - Eキーで終了
ができます。

### Color(CPU,Mem)
CPU、Memのテキストカラーの色は、
 - 緑・・・0~20%
 - 黄・・・21~70%
 - 赤・・・71~90%
 - 背景赤・91~100%
を表しています<br>

### Color(Internet)
Internetのテキストカラーの色は、
 - 赤・・・非アクティブ
 - 緑・・・アクティブ
を表しています<br>


## CopyRight
Copyright © 2024 Syobosyobonn(Zisty) All Right Reserved.
