using System;
using System.Text;

namespace AutoDealerSphere.Server.Utils
{
    /// <summary>
    /// 文字種変換ユーティリティクラス
    /// </summary>
    public static class CharacterConverter
    {
        /// <summary>
        /// 車検証JSONデータの文字種を正規化します
        /// - 全角英数字、記号を半角に変換
        /// - 半角カナを全角に変換
        /// </summary>
        /// <param name="input">変換対象の文字列</param>
        /// <returns>変換後の文字列</returns>
        public static string? NormalizeVehicleData(string? input)
        {
            if (string.IsNullOrEmpty(input))
                return input;

            var result = new StringBuilder(input.Length);

            foreach (char c in input)
            {
                // 全角英数字を半角に変換
                if (c >= '０' && c <= '９')
                {
                    result.Append((char)(c - '０' + '0'));
                }
                else if (c >= 'Ａ' && c <= 'Ｚ')
                {
                    result.Append((char)(c - 'Ａ' + 'A'));
                }
                else if (c >= 'ａ' && c <= 'ｚ')
                {
                    result.Append((char)(c - 'ａ' + 'a'));
                }
                // 全角記号を半角に変換
                else if (c == '　') // 全角スペース
                {
                    result.Append(' ');
                }
                else if (c == '－' || c == 'ー') // 全角ハイフン、長音
                {
                    result.Append('-');
                }
                else if (c == '（')
                {
                    result.Append('(');
                }
                else if (c == '）')
                {
                    result.Append(')');
                }
                else if (c == '．')
                {
                    result.Append('.');
                }
                else if (c == '，')
                {
                    result.Append(',');
                }
                else if (c == '：')
                {
                    result.Append(':');
                }
                else if (c == '；')
                {
                    result.Append(';');
                }
                else if (c == '！')
                {
                    result.Append('!');
                }
                else if (c == '？')
                {
                    result.Append('?');
                }
                else if (c == '＋')
                {
                    result.Append('+');
                }
                else if (c == '＊')
                {
                    result.Append('*');
                }
                else if (c == '／')
                {
                    result.Append('/');
                }
                else if (c == '＝')
                {
                    result.Append('=');
                }
                // 半角カナを全角に変換
                else if (c >= 'ｦ' && c <= 'ﾟ')
                {
                    result.Append(ConvertHalfKanaToFullKana(c));
                }
                else
                {
                    result.Append(c);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// 半角カナを全角カナに変換
        /// </summary>
        private static string ConvertHalfKanaToFullKana(char halfKana)
        {
            // 半角カナから全角カナへのマッピング
            return halfKana switch
            {
                'ｦ' => "ヲ",
                'ｧ' => "ァ",
                'ｨ' => "ィ",
                'ｩ' => "ゥ",
                'ｪ' => "ェ",
                'ｫ' => "ォ",
                'ｬ' => "ャ",
                'ｭ' => "ュ",
                'ｮ' => "ョ",
                'ｯ' => "ッ",
                'ｰ' => "ー",
                'ｱ' => "ア",
                'ｲ' => "イ",
                'ｳ' => "ウ",
                'ｴ' => "エ",
                'ｵ' => "オ",
                'ｶ' => "カ",
                'ｷ' => "キ",
                'ｸ' => "ク",
                'ｹ' => "ケ",
                'ｺ' => "コ",
                'ｻ' => "サ",
                'ｼ' => "シ",
                'ｽ' => "ス",
                'ｾ' => "セ",
                'ｿ' => "ソ",
                'ﾀ' => "タ",
                'ﾁ' => "チ",
                'ﾂ' => "ツ",
                'ﾃ' => "テ",
                'ﾄ' => "ト",
                'ﾅ' => "ナ",
                'ﾆ' => "ニ",
                'ﾇ' => "ヌ",
                'ﾈ' => "ネ",
                'ﾉ' => "ノ",
                'ﾊ' => "ハ",
                'ﾋ' => "ヒ",
                'ﾌ' => "フ",
                'ﾍ' => "ヘ",
                'ﾎ' => "ホ",
                'ﾏ' => "マ",
                'ﾐ' => "ミ",
                'ﾑ' => "ム",
                'ﾒ' => "メ",
                'ﾓ' => "モ",
                'ﾔ' => "ヤ",
                'ﾕ' => "ユ",
                'ﾖ' => "ヨ",
                'ﾗ' => "ラ",
                'ﾘ' => "リ",
                'ﾙ' => "ル",
                'ﾚ' => "レ",
                'ﾛ' => "ロ",
                'ﾜ' => "ワ",
                'ﾝ' => "ン",
                'ﾞ' => "゛", // 濁点
                'ﾟ' => "゜", // 半濁点
                _ => halfKana.ToString()
            };
        }

        /// <summary>
        /// 半角カナの濁点・半濁点を結合して全角カナに変換
        /// </summary>
        public static string NormalizeWithDakuten(string input)
        {
            if (string.IsNullOrEmpty(input))
                return input ?? string.Empty;

            var result = new StringBuilder();

            for (int i = 0; i < input.Length; i++)
            {
                char current = input[i];

                // 濁点・半濁点の処理
                if (i + 1 < input.Length)
                {
                    char next = input[i + 1];

                    // 濁点付き
                    if (next == 'ﾞ')
                    {
                        string converted = ConvertWithDakuten(current);
                        if (converted != current.ToString())
                        {
                            result.Append(converted);
                            i++; // 濁点をスキップ
                            continue;
                        }
                    }
                    // 半濁点付き
                    else if (next == 'ﾟ')
                    {
                        string converted = ConvertWithHandakuten(current);
                        if (converted != current.ToString())
                        {
                            result.Append(converted);
                            i++; // 半濁点をスキップ
                            continue;
                        }
                    }
                }

                // 通常の変換
                if (current >= 'ｦ' && current <= 'ﾟ')
                {
                    result.Append(ConvertHalfKanaToFullKana(current));
                }
                else
                {
                    result.Append(current);
                }
            }

            return result.ToString();
        }

        /// <summary>
        /// 濁点付き半角カナを全角カナに変換
        /// </summary>
        private static string ConvertWithDakuten(char halfKana)
        {
            return halfKana switch
            {
                'ｶ' => "ガ",
                'ｷ' => "ギ",
                'ｸ' => "グ",
                'ｹ' => "ゲ",
                'ｺ' => "ゴ",
                'ｻ' => "ザ",
                'ｼ' => "ジ",
                'ｽ' => "ズ",
                'ｾ' => "ゼ",
                'ｿ' => "ゾ",
                'ﾀ' => "ダ",
                'ﾁ' => "ヂ",
                'ﾂ' => "ヅ",
                'ﾃ' => "デ",
                'ﾄ' => "ド",
                'ﾊ' => "バ",
                'ﾋ' => "ビ",
                'ﾌ' => "ブ",
                'ﾍ' => "ベ",
                'ﾎ' => "ボ",
                'ｳ' => "ヴ",
                _ => halfKana.ToString()
            };
        }

        /// <summary>
        /// 半濁点付き半角カナを全角カナに変換
        /// </summary>
        private static string ConvertWithHandakuten(char halfKana)
        {
            return halfKana switch
            {
                'ﾊ' => "パ",
                'ﾋ' => "ピ",
                'ﾌ' => "プ",
                'ﾍ' => "ペ",
                'ﾎ' => "ポ",
                _ => halfKana.ToString()
            };
        }
    }
}
