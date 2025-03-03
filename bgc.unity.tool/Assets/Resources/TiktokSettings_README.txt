TikTok設定ファイルの使い方
====================

1. TiktokSettings.asset - 実際の設定ファイル（.gitignoreに追加してください）
   - APIキーなどの機密情報を含むため、Gitにコミットしないでください

2. TiktokSettings.sample.asset - サンプル設定ファイル
   - 設定ファイルの構造を示すためのサンプルです
   - APIキーは含まれていません
   - このファイルはGitにコミットして構いません

新しい環境でセットアップする場合：
1. TiktokSettings.sample.assetをコピーしてTiktokSettings.assetを作成
2. TiktokSettings.assetにAPIキーを設定
3. アプリケーションを実行
