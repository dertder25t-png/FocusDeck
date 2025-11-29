from playwright.sync_api import sync_playwright

def verify_minimized_window():
    with sync_playwright() as p:
        browser = p.chromium.launch(headless=True)
        context = browser.new_context(viewport={'width': 1280, 'height': 720})
        page = context.new_page()

        # Navigate to the app (assuming it runs on port 5173 based on Vite defaults)
        page.goto("http://localhost:5173")

        # Wait for the dashboard window to be visible
        page.wait_for_selector("#win-dashboard")

        # Take a screenshot before minimizing
        page.screenshot(path="/home/jules/verification/before_minimize.png")
        print("Screenshot taken: before_minimize.png")

        # Find the minimize button (it has the fa-minus icon) and click it
        minimize_btn = page.locator("#win-dashboard button i.fa-minus").locator("..")
        minimize_btn.click()

        # Wait for the window to be hidden (class hidden-app or display none)
        # The window element still exists but should have 'hidden-app' class
        dashboard_window = page.locator("#win-dashboard")
        # Check if it has the class 'hidden-app'
        assert "hidden-app" in dashboard_window.get_attribute("class")
        print("Window minimized successfully")

        # Take a screenshot after minimizing
        page.screenshot(path="/home/jules/verification/after_minimize.png")
        print("Screenshot taken: after_minimize.png")

        # Now restore it by clicking the taskbar item
        # The taskbar item for dashboard should be visible and have opacity-50 (minimized style)
        # But wait, in Taskbar.tsx, minimized items have opacity-50
        taskbar_item = page.locator("#task-dock button").first

        # Click to restore
        taskbar_item.click()

        # Check if window is visible again (no hidden-app class)
        # Allow a small delay for state update
        page.wait_for_timeout(500)
        assert "hidden-app" not in dashboard_window.get_attribute("class")
        print("Window restored successfully")

        # Take a screenshot after restore
        page.screenshot(path="/home/jules/verification/after_restore.png")
        print("Screenshot taken: after_restore.png")

        browser.close()

if __name__ == "__main__":
    verify_minimized_window()
