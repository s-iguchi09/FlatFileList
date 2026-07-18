namespace FlatFileList.Properties {
    
    
    // このクラスでは設定クラスでの特定のイベントを処理することができます:
    //  SettingChanging イベントは、設定値が変更される前に発生します。
    //  PropertyChanged イベントは、設定値が変更された後に発生します。
    //  SettingsLoaded イベントは、設定値が読み込まれた後に発生します。
    //  SettingsSaving イベントは、設定値が保存される前に発生します。
    /// <summary>
    /// アプリケーション設定クラスのユーザーコード側パーシャル。設定値の変更・保存イベントを処理できる。
    /// </summary>
    internal sealed partial class Settings {

        /// <summary>
        /// 設定クラスを初期化する。必要に応じて設定変更・保存のイベントハンドラーを登録する。
        /// </summary>
        public Settings() {
            // // 設定の保存と変更のイベント ハンドラーを追加するには、以下の行のコメントを解除します:
            //
            // this.SettingChanging += this.SettingChangingEventHandler;
            //
            // this.SettingsSaving += this.SettingsSavingEventHandler;
            //
        }
        
        /// <summary>
        /// 設定値が変更される前に呼び出されるイベントハンドラー。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">変更される設定に関する情報。</param>
        private void SettingChangingEventHandler(object sender, System.Configuration.SettingChangingEventArgs e) {
            // SettingChangingEvent イベントを処理するコードをここに追加してください。
        }
        
        /// <summary>
        /// 設定値が保存される前に呼び出されるイベントハンドラー。
        /// </summary>
        /// <param name="sender">イベントの発生元。</param>
        /// <param name="e">保存をキャンセルするための情報。</param>
        private void SettingsSavingEventHandler(object sender, System.ComponentModel.CancelEventArgs e) {
            // SettingsSaving イベントを処理するコードをここに追加してください。
        }
    }
}
