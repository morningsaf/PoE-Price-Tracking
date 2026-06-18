import requests
from bs4 import BeautifulSoup
import random
from pathlib import Path
import config
import sys
from pathlib import Path

sys.path.insert(0, str(Path(__file__).parent.parent))
from models import get_session, Items

session = requests.Session()
session.headers.update(
    {
        "Accept": "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,*/*;q=0.8",
        "Accept-Language": "en-US,en;q=0.9,ru;q=0.8",
        "Accept-Encoding": "gzip, deflate, br",
        "Connection": "keep-alive",
        "Upgrade-Insecure-Requests": "1",
    }
)
def download_image(url, filepath):
    response = session.get(url, stream=True, timeout=15)
    response.raise_for_status()
    with open(filepath, "wb") as f:
        for chunk in response.iter_content(chunk_size=8192):
            f.write(chunk)

def get_page(url, timeout = 15):
    user_agent = random.choice(config.USER_AGENTS)
    response = session.get(url, headers={"User-Agent": user_agent}, timeout=(10, timeout))
    response.raise_for_status()
    return response

def parse_page(url, item_type, sub_type):
    bd_session = get_session()
    response = get_page(url)
    soup = BeautifulSoup(response.text, "lxml")
    data = soup.select("span.hoverbox__activator.c-item-hoverbox__activator")
    for item in data:
        item_name = item.select_one("a[title]")["title"]
        if bd_session.query(Items).filter(Items.name == item_name).first():
            continue
        image = "https://www.poewiki.net" + item.select_one("img")["src"]
        filepath = config.MAIN_DIR / "images" /f"{item_name}_orig.png"
        download_image(image, filepath)
        bd_session.add(Items(name=item_name, item_type=item_type, item_sub_type=sub_type))
    bd_session.commit()
    bd_session.close()

for item_type, subtype in config.CATEGORIES.items():
    for sub_type, slug in subtype.items():
        url = config.WIKI_PATH + slug
        parse_page(url, item_type, sub_type)
        
