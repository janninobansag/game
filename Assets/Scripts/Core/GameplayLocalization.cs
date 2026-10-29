// PURPOSE: Provides translated gameplay subtitle text and helpers for supported game languages.
using System.Collections.Generic;
using UnityEngine;

public static class GameplayLocalization
{
    // English source text is used as the lookup key for each available language.
    private static readonly Dictionary<string, string> koreanSubtitles = new Dictionary<string, string>
    {
        { "1989.", "1989년." },
        { "Malawak Forest was once a living village.", "말라왝 숲은 한때 삶아 있던 마을이었다." },
        { "Father Mateo guided its people.", "마테오 신부가 마을 사람들을 이끌었다." },
        { "An ancient shadow named Varen possessed Father Mateo.", "바렌이라는 고대의 그림자가 마테오 신부를 빙의했다." },
        { "It prepared a ritual to claim his body forever.", "녀석은 그의 몸을 영원히 차지하려 의식을 준비했다." },
        { "During a prayer in the church, Father Mateo begged everyone to stop.", "교회의 기도 중, 마테오 신부는 멈추라고 애원했다." },
        { "The ritual broke.", "의식은 무너졌다." },
        { "Varen killed the people gathered inside.", "바렌은 안에 모인 사람들을 죽였다." },
        { "Mil and Jude escaped with candles, a Bible, and a cross.", "밀과 주드는 초, 성경, 십자가를 가지고 도망쳤다." },
        { "They purified the sacred items.", "두 사람은 성물들을 정화했다." },
        { "But Varen found them.", "하지만 바렌이 그들을 찾았다." },
        { "Jude died outside House 3. Mil died inside.", "주드는 3번 집 밖에서 죽고, 밀은 집 안에서 죽었다." },
        { "Laica became the White Lady.", "라이카는 흰 여인이 되었다." },
        { "Jude became the Tikbalang.", "주드는 티크발랑이 되었다." },
        { "Both spirits could not accept their deaths.", "두 영혼은 자신의 죽음을 받아들이지 못했다." },
        { "Now, an adventurer on vacation finds the abandoned village.", "이제 휴가 중인 모험가가 버려진 마을을 발견한다." },
        { "Explore the guard house, the three homes, and the church.", "경비실, 세 채의 집, 그리고 교회를 탐색하라." },
        { "Return the sacred items. Pray at the Ritual Tree.", "성물들을 돌려놓고 의식의 나무에서 기도하라." },
        { "Seal Varen before Malawak Forest claims you.", "말라왝 숲이 당신을 삼키기 전에 바렌을 봉인하라." },        { "This place… doesn’t feel right.", "이곳은... 뭔가 이상해." },
        { "this house…", "이 집은..." },
        { "WTF WAS THAT!!", "방금 뭐였어?!" },
        { "This is where it started…", "여기서 모든 일이 시작됐어..." },
        { "Someone lived here recently…", "최근까지 누군가 여기 살았어..." },
        { "Who’s there?!", "거기 누구야?!" },
        { "Stop messing with me…", "나를 그만 괴롭혀..." },
        { "“A ritual… to keep something trapped?", "무언가를 가두기 위한 의식인가?" },
        { "No lights… no movement…", "불빛도, 움직임도 없어..." },
        { "…I don’t like this…", "...이건 좋지 않아..." },
        { "This must be church…", "여기가 교회구나..." },
        { "They were trying to keep people out… why?", "들어오지 못하게 했어... 왜지?" },
        { "These must be the items…", "이것들이 그 아이템들인 것 같아..." },
        { "It’s not in the church anymore…", "더 이상 교회에 있지 않아..." },
        { "This place feels worse…", "여기는 더 나빠 느껴져..." },
        { "Good… I'll need this.", "좋아... 이건 필요해." },
        { "A key?… maybe I can use this somewhere.", "열쇠? ... 어디에서 쓸 수 있겠어." },
        { "Left Click: Continue | ENTER: Skip", "왼쪽 클릭: 계속 | ENTER: 건너뛰기" },
        { "The lights are out! Find the Generator and fix it as soon as possible.", "불이 꺼졌습니다! 가능한 빨리 발전기를 찾아 고치세요." },
        { "WARNING: Generator power is failing! Find fuel quickly before the lights go out.", "경고: 발전기 전력이 약해지고 있습니다! 불이 꺼지기 전에 빨리 연료를 찾으세요." },
        { "Generator Key required", "발전기 열쇠가 필요합니다" },
        { "Press E to insert Generator Key", "E 키를 눌러 발전기 열쇠를 넣으세요" },
        { "Fuel required", "연료가 필요합니다" },
        { "Press E to turn Generator on", "E 키를 눌러 발전기를 켜세요" },
        { "Wrench required", "렌치가 필요합니다" },
        { "Hold E to use Wrench", "E 키를 길게 눌러 렌치를 사용하세요" },
        { "Remove the gas tank cover first", "먼저 연료 탱크 덮개를 제거하세요" },
        { "Hold {0} to pour Gas: {1}%", "{0} 키를 길게 눌러 연료를 붓는 중: {1}%" },
        { "Hold {0} to pour Gas", "{0} 키를 길게 눌러 연료 붓기" },
        { "Hold {0} to unscrew Gas Tank Cover: {1}%", "{0} 키를 길게 눌러 연료 탱크 덮개 풀기: {1}%" },
        { "Time: {0:F1}s", "시간: {0:F1}초" },
        { "Correct!", "정답!" },
        { "Wrong!", "오답!" },
        { "What Philippine mythical creature is half-human and half-horse?", "반은 인간이고 반은 말인 필리핀 신화 속 생물은 무엇인가요?" },
        { "What creature is distinguished from many other aswang traditions by its ability to separate its upper torso from its lower body?", "상반신과 하반신을 분리할 수 있는 특징으로 알려진 필리핀 신화 속 생물은 무엇인가요?" },
        { "What term refers to a practitioner believed to cause supernatural harm through curses or sorcery in some Filipino traditions?", "일부 필리핀 전통에서 저주나 주술로 초자연적 해를 끼친다고 믿는 사람을 가리키는 말은 무엇인가요?" },
        { "What is the Philippine mythical creature that is half-human, half-horse?", "반은 인간이고 반은 말인 필리핀 신화 속 생물은 무엇인가요?" },
        { "What is the name of the tree-dwelling giant in Filipino folklore?", "필리핀 민속에서 나무에 사는 거인의 이름은 무엇인가요?" },
        { "Manananggal", "마나낭갈" },
        { "Diwata", "디와타" },
        { "Tikbalang", "티크발랑" },
        { "Kapre", "카프레" },
        { "Amalanhig", "아말란힌" },
        { "Mangkukulam", "망쿠쿨람" },
        { "Bakunawa", "바쿠나와" },
        { "Tiyanak", "티야낙" },
        { "Enkanto", "엔칸토" },
        { "White Lady", "백색 여인" }
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
        { "This place… doesn’t feel right.", "May mali sa lugar na ito..." },
        { "this house…", "ang bahay na ito..." },
        { "WTF WAS THAT!!", "ANO 'YON?!" },
        { "This is where it started…", "Dito nagsimula ang lahat..." },
        { "Someone lived here recently…", "May nakatira rito kamakailan..." },
        { "Who’s there?!", "Sino 'yan?!" },
        { "Stop messing with me…", "Tigilan mo ako..." },
        { "“A ritual… to keep something trapped?", "Isang ritwal... para ikulong ang isang bagay?" },
        { "No lights… no movement…", "Walang ilaw... walang gumagalaw..." },
        { "…I don’t like this…", "...Hindi ko gusto ito..." },
        { "This must be church…", "Ito na siguro ang simbahan..." },
        { "They were trying to keep people out… why?", "Sinusubukan nilang pigilan ang mga tao na pumasok... bakit?" },
        { "These must be the items…", "Ito na siguro ang mga gamit..." },
        { "It’s not in the church anymore…", "Wala na ito sa simbahan..." },
        { "This place feels worse…", "Mas masama ang pakiramdam sa lugar na ito..." },
        { "Good… I'll need this.", "Mabuti... kakailanganin ko ito." },
        { "A key?… maybe I can use this somewhere.", "Isang susi? ... Baka magagamit ko ito sa ibang lugar." },
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
