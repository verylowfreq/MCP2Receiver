# MCP2Receiver
Simple implementation of receiving SMF (Sony Motion Format; mocopi's UDP) motion data for Unity.

SMF形式（mocopiのUDP通信）でモーションデータを受け取る、Unity向けC#コードの簡易実装です。

公式SDKはバイナリライブラリを含むため一部のプラットフォーム（Android 32bit ARM）では利用できません。その簡易的な代替として作りました。

通信フォーマットの情報はmocopi公式Discordにて公開されている "SonyMotionFormat.pdf" (2024-04-05) をもとにしています。


## How to use / 使い方

 1. MCP2Avatar をシーン内に配置してください。
 1. Avatarに駆動対象のアバター (Animator) を設定してください。
 1. AutoStart を有効にします

**アプリケーション起動後にポート番号を変更するとき:**

StartListen() を呼び出してください。既存のポートをいったん閉じて、新しいポート番号で待ち受けます。

## License

MIT License (c) 2024 Mitsumine Suzu (verylowfreq)
