
import time
from playwright.sync_api import sync_playwright

def verify_mocked_ui():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        context = browser.new_context(ignore_https_errors=True)
        page = context.new_page()

        # Mock API Responses to bypass auth/backend requirement for UI testing
        # 1. Notes API
        page.route("**/api/notes**", lambda route: route.fulfill(
            status=200,
            content_type="application/json",
            body='[{"id": "1", "title": "Mock Note 1", "content": "This is a mock note.", "createdDate": "2023-01-01T00:00:00Z", "tags": ["test"]}]'
        ))

        # 2. Decks API (Flashcards)
        page.route("**/api/decks**", lambda route: route.fulfill(
            status=200,
            content_type="application/json",
            body='[{"id": "d1", "name": "Mock Deck", "cards": [{"id": "c1", "front": "Q1", "back": "A1"}]}]'
        ))

        # 3. Emails API (Integrations)
        page.route("**/v1/integrations/google/messages", lambda route: route.fulfill(
            status=200,
            content_type="application/json",
            body='[{"id": "e1", "sender": "Mock Sender", "subject": "Mock Subject", "snippet": "Hello world", "date": "10:00 AM", "isRead": false}]'
        ))

        # 4. Auth Refresh (Prevent loop/redirect)
        # We want api.ts to think token is valid or refresh works?
        # Actually api.ts redirects if refresh fails.
        # So we mock refresh to return a token.
        page.route("**/v1/auth/refresh", lambda route: route.fulfill(
            status=200,
            content_type="application/json",
            body='{"accessToken": "mock_access_token", "refreshToken": "mock_refresh_token"}'
        ))

        # 5. Privacy Consent (Prevent context error)
        page.route("**/v1/privacy/consent", lambda route: route.fulfill(
            status=200,
            content_type="application/json",
            body='[]'
        ))

        print("Navigating to App with Mocks...")

        # Inject dummy token so getAuthToken() passes locally
        # We need to do this *before* app loads, so use add_init_script
        page.add_init_script("""
            localStorage.setItem('focusdeck_access_token', 'mock_jwt_token_header.mock_payload.mock_signature');
            localStorage.setItem('focusdeck_refresh_token', 'mock_refresh');
        """)

        try:
            # Go to specific routes if handled by Router, or just root if DesktopLayout manages it.
            # DesktopLayout renders ALL windows hidden/visible based on context.
            # We need to trigger window visibility.
            # But the URL routing in App.tsx was: <Route path="/*" element={<DesktopLayout />} />
            # It seems DesktopLayout handles windows internally via WindowManagerContext.
            # We might need to click the Taskbar icons to "open" apps.

            page.goto("http://localhost:5173/", timeout=15000)
            time.sleep(3) # Wait for initial load

            # Screenshot Desktop
            page.screenshot(path="verify_desktop.png")
            print("Desktop loaded.")

            # Click Notes Icon (assuming taskbar has it)
            # Need to find selector. Looking at DesktopLayout/Taskbar code would help,
            # but let's assume standard aria-labels or text.
            # If not, we can inspect Taskbar.tsx (which I haven't read yet).
            # I read DesktopLayout.tsx, it imports Taskbar.

            # Let's blindly try to click buttons in the footer/taskbar.
            # Or use the "Tool Picker" if visible.

            # Better strategy: Force window open via React context is hard from outside.
            # Let's try finding the window elements directly.
            # <Window id="win-notes"> renders <NotesApp /> inside.
            # If the window is "minimized" or closed, it might be hidden via CSS.
            # Let's inspect the DOM for "win-notes".

            # Take screenshot of whatever is visible
            page.screenshot(path="verify_ui_state.png")

        except Exception as e:
            print(f"Navigation failed: {e}")

        browser.close()

if __name__ == "__main__":
    verify_mocked_ui()
