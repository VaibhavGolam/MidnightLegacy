races.json lets you change or add events without touching code.

Each entry in "races" is matched by "id". If the id already exists in the game (see ContentDB.cs) the entry
replaces it, otherwise it is added to the side events list. Only fill in the fields you want; the rest take the defaults.

Example (copy into races.json):

{
  "races": [
    {
      "id": "s_harbor_dash",
      "title": "Harbor Dash (hard)",
      "kind": 1,
      "theme": "forest",
      "time": "night",
      "weather": "rain",
      "seed": 4242,
      "length": 1800,
      "rival": "rook",
      "rewardCash": 400,
      "rewardRep": 20,
      "difficulty": 0.6
    }
  ]
}

kind: 0 Tutorial, 1 Sprint, 2 TimeTrial, 3 DriftScore, 4 TrafficRun, 5 Chase, 6 Escape, 7 Endless
theme: forest, pass, coast, city      time: night, midnight, dusk, dawn      weather: clear, rain, fog, storm
rival: street1, street2, rook, ivy, dex, enforcer, unknown
