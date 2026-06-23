import requests
import config
import sys
import json
import argparse
from pathlib import Path
import time

sys.path.insert(0, str(Path(__file__).parent.parent))
from models import get_session, Items, Price
from currency_rate import fetch_currency_rates

parser = argparse.ArgumentParser()
parser.add_argument("--items", required=True, help="comma-separated item names")
parser.add_argument("--league", default="Standard", help="league name")
args = parser.parse_args()

names = args.items.split(",")
league = args.league
rates = fetch_currency_rates(league)

session = requests.Session()
session.trust_env = False
session.cookies.set("POESESSID", config.POESESSID)
session.headers.update({
    "User-Agent": "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/146.0.0.0 Safari/537.36",
    "Accept": "application/json",
    "X-Requested-With": "XMLHttpRequest",
    "Content-Type": "application/json",
})

post_url = f"https://www.pathofexile.com/api/trade/search/{league}"

def take_search_id(name):
    body = {
        "query": {
            "status": {"option": "securable"},
            "name": name,
            "stats": [{"type": "and", "filters": [], "disabled": False}],
        },
        "sort": {"price": "asc"},
    }
    response = session.post(post_url, json=body,timeout=5)
    if response.status_code == 429:
        retry_after = response.headers.get("Retry-After", "unknown")
        return {"error": "ban", "retry_after": retry_after}
    return response.json()

def take_price(data, name):
    if data is None or not data.get("result"):
        return None
    prices_response = session.get(f"https://www.pathofexile.com/api/trade/fetch/{(data["result"][0])}?query={data["id"]}", timeout=5)
    price = prices_response.json()
    return {"name": name, "amount": price["result"][0]["listing"]["price"]["amount"], "currency": price["result"][0]["listing"]["price"]["currency"]}

def save_price(item_name, amount, currency):
    session = get_session()
    item = session.query(Items).filter(Items.name == item_name).first()
    if item is None:
        session.close()
        return None
    last_price = session.query(Price).filter(Price.item_id == item.id, Price.league == league).order_by(Price.id.desc()).first()
    prev_chaos_equal = last_price.chaos_equal if last_price else None
    chaos_equal = amount * rates.get(currency, 1.0)
    record = Price(item_id = item.id, price = amount, currency = currency, chaos_equal = chaos_equal, prev_chaos_equal = prev_chaos_equal, league = league)
    session.add(record)
    session.commit()
    session.close()
    return chaos_equal, prev_chaos_equal

time.sleep(2)
for name in names:
    pos = take_search_id(name)
    if "error" in pos:
        print(json.dumps({"error": "ban", "retry_after": pos["retry_after"]}), flush=True)
        sys.exit(0)
        continue

    price_data = take_price(pos, name)
    if price_data is None:
        result = ({"name": name, "amount": None, "currency": None, "trend": "none"})
        print(json.dumps(result), flush=True)
        continue

    chaos_equal, prev_chaos_equal = save_price(name, price_data["amount"], price_data["currency"])
    if prev_chaos_equal is None:
        trend = ""
    elif chaos_equal > prev_chaos_equal:
        trend = f"+{round(((chaos_equal - prev_chaos_equal) / prev_chaos_equal) * 100, 2)}%"
    elif chaos_equal < prev_chaos_equal:
        trend = f"{round(((chaos_equal - prev_chaos_equal) / prev_chaos_equal) * 100, 2)}%"
    else:
        trend = "+0.00%"
    result = {"name": name, "amount": price_data["amount"], "currency" : price_data["currency"], "trend" : trend, "query_id" : pos["id"]}
    print(json.dumps(result), flush=True)
    time.sleep(2)
