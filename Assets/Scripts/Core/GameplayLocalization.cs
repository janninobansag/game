// PURPOSE: Provides translated gameplay subtitle text and helpers for supported game languages.
using System.Collections.Generic;
using UnityEngine;

public static class GameplayLocalization
{
    // English source text is used as the lookup key for each available language.
    private static readonly Dictionary<string, string> koreanSubtitles = new Dictionary<string, string>
    {
        { "1989.", "1989\uB144." },
        { "Malawak Forest was once a living village.", "\uB9D0\uB77C\uC65D \uC232\uC740 \uD55C\uB54C \uC0B6\uC544 \uC788\uB358 \uB9C8\uC744\uC774\uC5C8\uB2E4." },
        { "Father Mateo guided its people.", "\uB9C8\uD14C\uC624 \uC2E0\uBD80\uAC00 \uB9C8\uC744 \uC0AC\uB78C\uB4E4\uC744 \uC774\uB04C\uC5C8\uB2E4." },
        { "An ancient shadow named Varen possessed Father Mateo.", "\uBC14\uB80C\uC774\uB77C\uB294 \uACE0\uB300\uC758 \uADF8\uB9BC\uC790\uAC00 \uB9C8\uD14C\uC624 \uC2E0\uBD80\uB97C \uBE59\uC758\uD588\uB2E4." },
        { "It prepared a ritual to claim his body forever.", "\uB140\uC11D\uC740 \uADF8\uC758 \uBAB8\uC744 \uC601\uC6D0\uD788 \uCC28\uC9C0\uD558\uB824 \uC758\uC2DD\uC744 \uC900\uBE44\uD588\uB2E4." },
        { "During a prayer in the church, Father Mateo begged everyone to stop.", "\uAD50\uD68C\uC758 \uAE30\uB3C4 \uC911, \uB9C8\uD14C\uC624 \uC2E0\uBD80\uB294 \uBA48\uCD94\uB77C\uACE0 \uC560\uC6D0\uD588\uB2E4." },
        { "The ritual broke.", "\uC758\uC2DD\uC740 \uBB34\uB108\uC84C\uB2E4." },
        { "Varen killed the people gathered inside.", "\uBC14\uB80C\uC740 \uC548\uC5D0 \uBAA8\uC778 \uC0AC\uB78C\uB4E4\uC744 \uC8FD\uC600\uB2E4." },
        { "Mil and Jude escaped with candles, a Bible, and a cross.", "\uBC00\uACFC \uC8FC\uB4DC\uB294 \uCD08, \uC131\uACBD, \uC2ED\uC790\uAC00\uB97C \uAC00\uC9C0\uACE0 \uB3C4\uB9DD\uCCE4\uB2E4." },
        { "They purified the sacred items.", "\uB450 \uC0AC\uB78C\uC740 \uC131\uBB3C\uB4E4\uC744 \uC815\uD654\uD588\uB2E4." },
        { "But Varen found them.", "\uD558\uC9C0\uB9CC \uBC14\uB80C\uC774 \uADF8\uB4E4\uC744 \uCC3E\uC558\uB2E4." },
        { "Jude died outside House 3. Mil died inside.", "\uC8FC\uB4DC\uB294 3\uBC88 \uC9D1 \uBC16\uC5D0\uC11C \uC8FD\uACE0, \uBC00\uC740 \uC9D1 \uC548\uC5D0\uC11C \uC8FD\uC5C8\uB2E4." },
        { "Laica became the White Lady.", "\uB77C\uC774\uCE74\uB294 \uD770 \uC5EC\uC778\uC774 \uB418\uC5C8\uB2E4." },
        { "Jude became the Tikbalang.", "\uC8FC\uB4DC\uB294 \uD2F0\uD06C\uBC1C\uB791\uC774 \uB418\uC5C8\uB2E4." },
        { "Both spirits could not accept their deaths.", "\uB450 \uC601\uD63C\uC740 \uC790\uC2E0\uC758 \uC8FD\uC74C\uC744 \uBC1B\uC544\uB4E4\uC774\uC9C0 \uBABB\uD588\uB2E4." },
        { "Now, an adventurer on vacation finds the abandoned village.", "\uC774\uC81C \uD734\uAC00 \uC911\uC778 \uBAA8\uD5D8\uAC00\uAC00 \uBC84\uB824\uC9C4 \uB9C8\uC744\uC744 \uBC1C\uACAC\uD55C\uB2E4." },
        { "Explore the guard house, the three homes, and the church.", "\uACBD\uBE44\uC2E4, \uC138 \uCC44\uC758 \uC9D1, \uADF8\uB9AC\uACE0 \uAD50\uD68C\uB97C \uD0D0\uC0C9\uD558\uB77C." },
        { "Return the sacred items. Pray at the Ritual Tree.", "\uC131\uBB3C\uB4E4\uC744 \uB3CC\uB824\uB193\uACE0 \uC758\uC2DD\uC758 \uB098\uBB34\uC5D0\uC11C \uAE30\uB3C4\uD558\uB77C." },
        { "Seal Varen before Malawak Forest claims you.", "\uB9D0\uB77C\uC65D \uC232\uC774 \uB2F9\uC2E0\uC744 \uC0BC\uD0A4\uAE30 \uC804\uC5D0 \uBC14\uB80C\uC744 \uBD09\uC778\uD558\uB77C." },        { "This place\u2026 doesn\u2019t feel right.", "\uC774\uACF3\uC740... \uBB54\uAC00 \uC774\uC0C1\uD574." },
        { "this house\u2026", "\uC774 \uC9D1\uC740..." },
        { "WTF WAS THAT!!", "\uBC29\uAE08 \uBB50\uC600\uC5B4?!" },
        { "This is where it started\u2026", "\uC5EC\uAE30\uC11C \uBAA8\uB4E0 \uC77C\uC774 \uC2DC\uC791\uB410\uC5B4..." },
        { "Someone lived here recently\u2026", "\uCD5C\uADFC\uAE4C\uC9C0 \uB204\uAD70\uAC00 \uC5EC\uAE30 \uC0B4\uC558\uC5B4..." },
        { "Who\u2019s there?!", "\uAC70\uAE30 \uB204\uAD6C\uC57C?!" },
        { "Stop messing with me\u2026", "\uB098\uB97C \uADF8\uB9CC \uAD34\uB86D\uD600..." },
        { "\u201CA ritual\u2026 to keep something trapped?", "\uBB34\uC5B8\uAC00\uB97C \uAC00\uB450\uAE30 \uC704\uD55C \uC758\uC2DD\uC778\uAC00?" },
        { "No lights\u2026 no movement\u2026", "\uBD88\uBE5B\uB3C4, \uC6C0\uC9C1\uC784\uB3C4 \uC5C6\uC5B4..." },
        { "\u2026I don\u2019t like this\u2026", "...\uC774\uAC74 \uC88B\uC9C0 \uC54A\uC544..." },
        { "This must be church\u2026", "\uC5EC\uAE30\uAC00 \uAD50\uD68C\uAD6C\uB098..." },
        { "They were trying to keep people out\u2026 why?", "\uB4E4\uC5B4\uC624\uC9C0 \uBABB\uD558\uAC8C \uD588\uC5B4... \uC65C\uC9C0?" },
        { "These must be the items\u2026", "\uC774\uAC83\uB4E4\uC774 \uADF8 \uC544\uC774\uD15C\uB4E4\uC778 \uAC83 \uAC19\uC544..." },
        { "It\u2019s not in the church anymore\u2026", "\uB354 \uC774\uC0C1 \uAD50\uD68C\uC5D0 \uC788\uC9C0 \uC54A\uC544..." },
        { "This place feels worse\u2026", "\uC5EC\uAE30\uB294 \uB354 \uB098\uBE60 \uB290\uAEF4\uC838..." },
        { "Good\u2026 I'll need this.", "\uC88B\uC544... \uC774\uAC74 \uD544\uC694\uD574." },
        { "A key?\u2026 maybe I can use this somewhere.", "\uC5F4\uC1E0? ... \uC5B4\uB514\uC5D0\uC11C \uC4F8 \uC218 \uC788\uACA0\uC5B4." },
        { "Left Click: Continue | ENTER: Skip", "\uC67C\uCABD \uD074\uB9AD: \uACC4\uC18D | ENTER: \uAC74\uB108\uB6F0\uAE30" },
        { "The lights are out! Find the Generator and fix it as soon as possible.", "\uBD88\uC774 \uAEBC\uC84C\uC2B5\uB2C8\uB2E4! \uAC00\uB2A5\uD55C \uBE68\uB9AC \uBC1C\uC804\uAE30\uB97C \uCC3E\uC544 \uACE0\uCE58\uC138\uC694." },
        { "WARNING: Generator power is failing! Find fuel quickly before the lights go out.", "\uACBD\uACE0: \uBC1C\uC804\uAE30 \uC804\uB825\uC774 \uC57D\uD574\uC9C0\uACE0 \uC788\uC2B5\uB2C8\uB2E4! \uBD88\uC774 \uAEBC\uC9C0\uAE30 \uC804\uC5D0 \uBE68\uB9AC \uC5F0\uB8CC\uB97C \uCC3E\uC73C\uC138\uC694." },
        { "Generator Key required", "\uBC1C\uC804\uAE30 \uC5F4\uC1E0\uAC00 \uD544\uC694\uD569\uB2C8\uB2E4" },
        { "Press E to insert Generator Key", "E \uD0A4\uB97C \uB20C\uB7EC \uBC1C\uC804\uAE30 \uC5F4\uC1E0\uB97C \uB123\uC73C\uC138\uC694" },
        { "Fuel required", "\uC5F0\uB8CC\uAC00 \uD544\uC694\uD569\uB2C8\uB2E4" },
        { "Press E to turn Generator on", "E \uD0A4\uB97C \uB20C\uB7EC \uBC1C\uC804\uAE30\uB97C \uCF1C\uC138\uC694" },
        { "Wrench required", "\uB80C\uCE58\uAC00 \uD544\uC694\uD569\uB2C8\uB2E4" },
        { "Hold E to use Wrench", "E \uD0A4\uB97C \uAE38\uAC8C \uB20C\uB7EC \uB80C\uCE58\uB97C \uC0AC\uC6A9\uD558\uC138\uC694" },
        { "Remove the gas tank cover first", "\uBA3C\uC800 \uC5F0\uB8CC \uD0F1\uD06C \uB36E\uAC1C\uB97C \uC81C\uAC70\uD558\uC138\uC694" },
        { "Hold {0} to pour Gas: {1}%", "{0} \uD0A4\uB97C \uAE38\uAC8C \uB20C\uB7EC \uC5F0\uB8CC\uB97C \uBD93\uB294 \uC911: {1}%" },
        { "Hold {0} to pour Gas", "{0} \uD0A4\uB97C \uAE38\uAC8C \uB20C\uB7EC \uC5F0\uB8CC \uBD93\uAE30" },
        { "Hold {0} to unscrew Gas Tank Cover: {1}%", "{0} \uD0A4\uB97C \uAE38\uAC8C \uB20C\uB7EC \uC5F0\uB8CC \uD0F1\uD06C \uB36E\uAC1C \uD480\uAE30: {1}%" },
        { "Time: {0:F1}s", "\uC2DC\uAC04: {0:F1}\uCD08" },
        { "Correct!", "\uC815\uB2F5!" },
        { "Wrong!", "\uC624\uB2F5!" },
        { "What Philippine mythical creature is half-human and half-horse?", "\uBC18\uC740 \uC778\uAC04\uC774\uACE0 \uBC18\uC740 \uB9D0\uC778 \uD544\uB9AC\uD540 \uC2E0\uD654 \uC18D \uC0DD\uBB3C\uC740 \uBB34\uC5C7\uC778\uAC00\uC694?" },
        { "What creature is distinguished from many other aswang traditions by its ability to separate its upper torso from its lower body?", "\uC0C1\uBC18\uC2E0\uACFC \uD558\uBC18\uC2E0\uC744 \uBD84\uB9AC\uD560 \uC218 \uC788\uB294 \uD2B9\uC9D5\uC73C\uB85C \uC54C\uB824\uC9C4 \uD544\uB9AC\uD540 \uC2E0\uD654 \uC18D \uC0DD\uBB3C\uC740 \uBB34\uC5C7\uC778\uAC00\uC694?" },
        { "What term refers to a practitioner believed to cause supernatural harm through curses or sorcery in some Filipino traditions?", "\uC77C\uBD80 \uD544\uB9AC\uD540 \uC804\uD1B5\uC5D0\uC11C \uC800\uC8FC\uB098 \uC8FC\uC220\uB85C \uCD08\uC790\uC5F0\uC801 \uD574\uB97C \uB07C\uCE5C\uB2E4\uACE0 \uBBFF\uB294 \uC0AC\uB78C\uC744 \uAC00\uB9AC\uD0A4\uB294 \uB9D0\uC740 \uBB34\uC5C7\uC778\uAC00\uC694?" },
        { "What is the Philippine mythical creature that is half-human, half-horse?", "\uBC18\uC740 \uC778\uAC04\uC774\uACE0 \uBC18\uC740 \uB9D0\uC778 \uD544\uB9AC\uD540 \uC2E0\uD654 \uC18D \uC0DD\uBB3C\uC740 \uBB34\uC5C7\uC778\uAC00\uC694?" },
        { "What is the name of the tree-dwelling giant in Filipino folklore?", "\uD544\uB9AC\uD540 \uBBFC\uC18D\uC5D0\uC11C \uB098\uBB34\uC5D0 \uC0AC\uB294 \uAC70\uC778\uC758 \uC774\uB984\uC740 \uBB34\uC5C7\uC778\uAC00\uC694?" },
        { "Manananggal", "\uB9C8\uB098\uB0AD\uAC08" },
        { "Diwata", "\uB514\uC640\uD0C0" },
        { "Tikbalang", "\uD2F0\uD06C\uBC1C\uB791" },
        { "Kapre", "\uCE74\uD504\uB808" },
        { "Amalanhig", "\uC544\uB9D0\uB780\uD78C" },
        { "Mangkukulam", "\uB9DD\uCFE0\uCFE8\uB78C" },
        { "Bakunawa", "\uBC14\uCFE0\uB098\uC640" },
        { "Tiyanak", "\uD2F0\uC57C\uB099" },
        { "Enkanto", "\uC5D4\uCE78\uD1A0" },
        { "White Lady", "\uBC31\uC0C9 \uC5EC\uC778" }
    };

    private static readonly Dictionary<string, string> tagalogSubtitles = new Dictionary<string, string>
    {
        { "1989.", "1989." },
        { "Malawak Forest was once a living village.", "Ang Malawak Forest ay dating isang masiglang nayon." },
        { "Father Mateo guided its people.", "Pinamunuan ni Padre Mateo ang mga mamamayan nito." },
        { "An ancient shadow named Varen possessed Father Mateo.", "Sinaniban ng sinaunang aninong si Varen si Padre Mateo." },
        { "It prepared a ritual to claim his body forever.", "Naghanda ito ng ritwal upang angkinin ang kanyang katawan magpakailanman." },
        { "During a prayer in the church, Father Mateo begged everyone to stop.", "Habang nagdarasal sa simbahan, nakiusap si Padre Mateo na itigil ng lahat ang ritwal." },
        { "The ritual broke.", "Napigil ang ritwal." },
        { "Varen killed the people gathered inside.", "Pinatay ni Varen ang mga taong nagtipon sa loob." },
        { "Mil and Jude escaped with candles, a Bible, and a cross.", "Nakatakas sina Mil at Jude dala ang mga kandila, Bibliya, at krus." },
        { "They purified the sacred items.", "Pinabanal nila ang mga sagradong gamit." },
        { "But Varen found them.", "Ngunit natagpuan sila ni Varen." },
        { "Jude died outside House 3. Mil died inside.", "Namatay si Jude sa labas ng Bahay 3. Namatay si Mil sa loob." },
        { "Laica became the White Lady.", "Naging White Lady si Laica." },
        { "Jude became the Tikbalang.", "Naging Tikbalang si Jude." },
        { "Both spirits could not accept their deaths.", "Hindi matanggap ng dalawang espiritu ang kanilang pagkamatay." },
        { "Now, an adventurer on vacation finds the abandoned village.", "Ngayon, natagpuan ng isang adventurer na nagbabakasyon ang abandonadong nayon." },
        { "Explore the guard house, the three homes, and the church.", "Galugarin ang guard house, ang tatlong bahay, at ang simbahan." },
        { "Return the sacred items. Pray at the Ritual Tree.", "Ibalik ang mga sagradong gamit. Magdasal sa Ritual Tree." },
        { "Seal Varen before Malawak Forest claims you.", "I-seal si Varen bago ka angkinin ng Malawak Forest." },
        { "This place\u2026 doesn\u2019t feel right.", "May mali sa lugar na ito..." },
        { "this house\u2026", "ang bahay na ito..." },
        { "WTF WAS THAT!!", "ANO 'YON?!" },
        { "This is where it started\u2026", "Dito nagsimula ang lahat..." },
        { "Someone lived here recently\u2026", "May nakatira rito kamakailan..." },
        { "Who\u2019s there?!", "Sino 'yan?!" },
        { "Stop messing with me\u2026", "Tigilan mo ako..." },
        { "\u201CA ritual\u2026 to keep something trapped?", "Isang ritwal... para ikulong ang isang bagay?" },
        { "No lights\u2026 no movement\u2026", "Walang ilaw... walang gumagalaw..." },
        { "\u2026I don\u2019t like this\u2026", "...Hindi ko gusto ito..." },
        { "This must be church\u2026", "Ito na siguro ang simbahan..." },
        { "They were trying to keep people out\u2026 why?", "Sinusubukan nilang pigilan ang mga tao na pumasok... bakit?" },
        { "These must be the items\u2026", "Ito na siguro ang mga gamit..." },
        { "It\u2019s not in the church anymore\u2026", "Wala na ito sa simbahan..." },
        { "This place feels worse\u2026", "Mas masama ang pakiramdam sa lugar na ito..." },
        { "Good\u2026 I'll need this.", "Mabuti... kakailanganin ko ito." },
        { "A key?\u2026 maybe I can use this somewhere.", "Isang susi? ... Baka magagamit ko ito sa ibang lugar." },
        { "Left Click: Continue | ENTER: Skip", "Kaliwang Click: Magpatuloy | ENTER: Laktawan" },
        { "The lights are out! Find the Generator and fix it as soon as possible.", "Patay ang mga ilaw! Hanapin at ayusin ang Generator sa lalong madaling panahon." },
        { "WARNING: Generator power is failing! Find fuel quickly before the lights go out.", "BABALA: Humihina ang kuryente ng Generator! Humanap agad ng Fuel bago mamatay ang mga ilaw." },
        { "Generator Key required", "Kailangan ang Generator Key" },
        { "Press E to insert Generator Key", "Pindutin ang E upang ipasok ang Generator Key" },
        { "Fuel required", "Kailangan ang Fuel" },
        { "Press E to turn Generator on", "Pindutin ang E upang buksan ang Generator" },
        { "Wrench required", "Kailangan ang Wrench" },
        { "Hold E to use Wrench", "Pindutin nang matagal ang E upang gamitin ang Wrench" },
        { "Remove the gas tank cover first", "Alisin muna ang takip ng tangke ng Gas" },
        { "Hold {0} to pour Gas: {1}%", "Pindutin nang matagal ang {0} upang magbuhos ng Gas: {1}%" },
        { "Hold {0} to pour Gas", "Pindutin nang matagal ang {0} upang magbuhos ng Gas" },
        { "Hold {0} to unscrew Gas Tank Cover: {1}%", "Pindutin nang matagal ang {0} upang tanggalin ang takip ng tangke ng Gas: {1}%" },
        { "Time: {0:F1}s", "Oras: {0:F1}s" },
        { "Correct!", "Tama!" },
        { "Wrong!", "Mali!" },
        { "What Philippine mythical creature is half-human and half-horse?", "Anong nilalang sa mitolohiyang Pilipino ang kalahating tao at kalahating kabayo?" },
        { "What creature is distinguished from many other aswang traditions by its ability to separate its upper torso from its lower body?", "Anong nilalang sa mitolohiyang Pilipino ang naiiba sa maraming tradisyon ng aswang dahil kaya nitong ihiwalay ang itaas na katawan nito sa ibabang katawan?" },
        { "What term refers to a practitioner believed to cause supernatural harm through curses or sorcery in some Filipino traditions?", "Anong tawag sa taong pinaniniwalaang nagdudulot ng supernatural na pinsala sa pamamagitan ng sumpa o kulam sa ilang tradisyong Pilipino?" },
        { "What is the Philippine mythical creature that is half-human, half-horse?", "Ano ang nilalang sa mitolohiyang Pilipino na kalahating tao at kalahating kabayo?" },
        { "What is the name of the tree-dwelling giant in Filipino folklore?", "Ano ang pangalan ng higanteng naninirahan sa puno sa kuwentong-bayan ng Pilipinas?" },
        { "White Lady", "Babaeng Puti" }
    };

    private static readonly Dictionary<string, string> koreanObjectives = new Dictionary<string, string>
    {
        { "Search the guard house.", "경비실을 수색하라." },
        { "Find the remaining ritual items", "남은 의식 물품을 찾아라." },
        { "You found the guard house.", "경비실을 찾았다." },
        { "bedroom!!", "침실!!" },
        { "Perform the ritual.", "의식을 수행하라." },
        { "Look for the house. Use the map \"M\"", "집을 찾아라. 지도는 \"M\" 키를 사용하라." },
        { "Candle placed on the vase", "꽃병에 촛불을 놓았다." },
        { "OBJECTIVE", "목표" },
        { "▶ NEW OBJECTIVE", "▶ 새로운 목표" }
    };

    private static readonly Dictionary<string, string> tagalogObjectives = new Dictionary<string, string>
    {
        { "Search the guard house.", "Hanapin ang guard house." },
        { "Find the remaining ritual items", "Hanapin ang natitirang mga gamit para sa ritwal." },
        { "You found the guard house.", "Nahanap mo ang guard house." },
        { "bedroom!!", "Silid-tulugan!!" },
        { "Perform the ritual.", "Isagawa ang ritwal." },
        { "Look for the house. Use the map \"M\"", "Hanapin ang bahay. Gamitin ang mapa gamit ang \"M\"." },
        { "Candle placed on the vase", "Inilagay ang kandila sa plorera." },
        { "OBJECTIVE", "LAYUNIN" },
        { "▶ NEW OBJECTIVE", "▶ BAGONG LAYUNIN" }
    };
    private static GameLanguage SelectedLanguage
    {
        get
        {
            SettingsData settings;
            if (SettingsDatabase.TryLoad(out settings))
                return (GameLanguage)Mathf.Clamp(settings.Language, 0, 2);

            return (GameLanguage)Mathf.Clamp(PlayerPrefs.GetInt("GameLanguage", 0), 0, 2);
        }
    }

    public static bool IsKorean => SelectedLanguage == GameLanguage.Korean;
    public static bool IsTagalog => SelectedLanguage == GameLanguage.Tagalog;
    public static bool IsLocalized => IsKorean || IsTagalog;


    public static string TranslateObjective(string english)
    {
        // Return the original text when no translation exists for the selected language.
        if (string.IsNullOrEmpty(english)) return english;

        string key = english.Trim();
        if (IsKorean)
            return koreanObjectives.TryGetValue(key, out string korean) ? korean : english;
        if (IsTagalog)
            return tagalogObjectives.TryGetValue(key, out string tagalog) ? tagalog : english;
        return english;
    }
    public static string TranslateSubtitle(string english)
    {
        // Return a translated subtitle only when a supported language is active.
        if (string.IsNullOrEmpty(english)) return english;

        if (IsKorean)
            return koreanSubtitles.TryGetValue(english, out string korean) ? korean : english;

        if (IsTagalog)
            return tagalogSubtitles.TryGetValue(english, out string tagalog) ? tagalog : english;

        return english;
    }
}
