using System;

namespace RomajiToHiraganaLibrary
{
    class RomajiHiraganaPair
    {
        /// <summary>
        /// ローマ字とそれに対応するひらがなを指定して、オブジェクトを初期化します。
        /// </summary>
        /// <param name="romaji">ローマ字</param>
        /// <param name="hiragana">ひらがな</param>
        public RomajiHiraganaPair(string romaji, string hiragana)
        {
            if(romaji == null) throw new ArgumentNullException("romaji");
            Romaji = romaji;

            if(romaji == null) throw new ArgumentNullException("hiragana");
            Hiragana = hiragana;
        }

        private string _romaji = "";
        /// <summary>
        /// ローマ字を取得、設定します。
        /// </summary>
        internal string Romaji { get { return _romaji; } set { _romaji = value; } }

        private string _hiragana = "";
        /// <summary>
        /// ひらがなを取得、設定します。
        /// </summary>
        internal string Hiragana { get { return _hiragana; } set { _hiragana = value; } }
    }
}
