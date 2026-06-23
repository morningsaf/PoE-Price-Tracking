import requests
from pathlib import Path
import sys
sys.path.insert(0, str(Path(__file__).parent.parent))
from scripts import config

def fetch_currency_rates(league):
    session = requests.Session()
    session.trust_env = False
    session.headers.update(
        {
            "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8",
            "Accept-Language": "en-US,en;q=0.9,ru;q=0.8",
            "Accept-Encoding": "gzip, deflate",
            "Connection": "keep-alive",
            "Upgrade-Insecure-Requests": "1",
        }
    )

    response = session.get(f"https://poe.ninja/poe1/api/economy/exchange/current/overview?league={league}&type=Currency")
    data = response.json()
    rates = {"chaos": 1.0}
    for cur in data["lines"]:
            rates[cur["id"]] = cur["primaryValue"]
    return rates




