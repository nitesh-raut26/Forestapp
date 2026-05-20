using UnityEngine;

namespace ForestFriendsQuest
{
    public static class ForestDataLoader
    {
        private const string ResourceName = "forest_game_content";

        public static ForestGameContent Load()
        {
            var contentAsset = Resources.Load<TextAsset>(ResourceName);

            if (contentAsset == null)
            {
                Debug.LogWarning(
                    $"Forest Friends Quest could not find Resources/{ResourceName}.json. Falling back to minimal data."
                );
                return CreateFallback();
            }

            var content = JsonUtility.FromJson<ForestGameContent>(contentAsset.text);

            if (content == null)
            {
                Debug.LogWarning("Forest Friends Quest data could not be parsed. Falling back to minimal data.");
                return CreateFallback();
            }

            return content;
        }

        private static ForestGameContent CreateFallback()
        {
            return new ForestGameContent
            {
                summary = new GameSummary
                {
                    ageBand = "Ages 4-8",
                    build = "Unity 2D MVP",
                    model = "Parent-gated premium",
                    title = "Forest Friends Quest",
                    tagline = "A cheerful forest puzzle adventure.",
                    sessionLength = "2-5 minute levels",
                    launchShape = "1 world, 3 puzzle types, 4 starter levels",
                },
                navigationTabs = new[]
                {
                    new NavigationTabData { id = "home", label = "World" },
                    new NavigationTabData { id = "play", label = "Play" },
                    new NavigationTabData { id = "parents", label = "Parents" },
                },
                characters = new[]
                {
                    new CharacterProfile
                    {
                        id = "pip",
                        name = "Pip",
                        role = "Forest scout",
                        accentHex = "#FFB36B",
                        blurb = "Curious fox guide",
                        voice = new CharacterVoiceData { pitch = 1.2f, rate = 0.95f },
                        lines = new CharacterLineData
                        {
                            greeting = "Hello explorer.",
                            hint = "Try the glowing clue.",
                            cheer = "You did it.",
                        },
                    },
                },
                zones = new[]
                {
                    new ForestZoneData
                    {
                        id = "fern-trail",
                        title = "Fern Trail",
                        mood = "Soft daylight and easy first wins.",
                        challenge = "Hidden object puzzles.",
                        reward = "Leaf star",
                        accentHex = "#A6D977",
                        unlockAfterClears = 0,
                        isPremium = false,
                        lockMessage = "Fern Trail is the first free zone.",
                    },
                },
                levels = new[]
                {
                    new LevelData
                    {
                        id = "level-01",
                        zoneId = "fern-trail",
                        characterId = "pip",
                        name = "Find Pip's Lantern",
                        type = "Hidden object",
                        difficulty = "Beginner",
                        reward = "Leaf star",
                        prompt = "Which item lights the trail?",
                        hint = "Look for the glowing forest item.",
                        celebration = "The trail is glowing again.",
                        gameplayMode = "choice",
                        options = new[]
                        {
                            new LevelOptionData
                            {
                                id = "berry-basket",
                                label = "Berry basket",
                                isCorrect = false,
                                reply = "Those berries are not the light source.",
                            },
                            new LevelOptionData
                            {
                                id = "lantern",
                                label = "Lantern",
                                isCorrect = true,
                                reply = "Yes. The lantern lights the path.",
                            },
                            new LevelOptionData
                            {
                                id = "pinecone",
                                label = "Pinecone",
                                isCorrect = false,
                                reply = "Nice find, but not the right clue.",
                            },
                        },
                    },
                },
                rewards = new[]
                {
                    new RewardMilestoneData
                    {
                        id = "reward-1",
                        levels = 1,
                        title = "Leaf star",
                        detail = "A bright reward for a first happy win.",
                    },
                },
                soundDesignPlan = new[]
                {
                    "Character cues use runtime-generated placeholder sounds.",
                    "Ambient birds, wind, water, and reward chimes can be swapped in later.",
                },
                researchHighlights = new[]
                {
                    "2D layered animation is the smartest first release path.",
                },
                parentFacingNotes = new[]
                {
                    "No harsh fail states in the first-session flow.",
                },
                monetizationPlan = new[]
                {
                    "Free early levels with a parent-gated one-time unlock.",
                },
                buildMilestones = new[]
                {
                    "Create a playable Unity vertical slice.",
                },
            };
        }
    }
}
