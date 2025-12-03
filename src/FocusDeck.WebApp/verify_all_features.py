
import time
from playwright.sync_api import sync_playwright

def verify_all_features():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        context = browser.new_context(ignore_https_errors=True)
        page = context.new_page()

        print("1. Testing Login Page...")
        try:
            page.goto("http://localhost:5173/login", timeout=10000)
            page.wait_for_selector("input[type='text']")
            print("   Login page loaded.")
            page.screenshot(path="verify_login.png")
        except Exception as e:
            print(f"   Login page failed: {e}")
            return

        print("2. Attempting to bypass auth for UI verification...")
        page.evaluate("""() => {
            localStorage.setItem('focusdeck_access_token', 'dummy_token');
            localStorage.setItem('focusdeck_refresh_token', 'dummy_refresh');
        }""")

        print("3. Testing Notes App...")
        try:
            page.goto("http://localhost:5173/notes", timeout=10000)
            time.sleep(2)
            print(f"   Current URL: {page.url}")

            if "login" in page.url:
                print("   Redirected to login (Expected if token validation fails).")
            else:
                page.screenshot(path="verify_notes.png")
                print("   Notes App loaded.")

        except Exception as e:
            print(f"   Notes App navigation failed: {e}")

        browser.close()

if __name__ == "__main__":
    verify_all_features()
