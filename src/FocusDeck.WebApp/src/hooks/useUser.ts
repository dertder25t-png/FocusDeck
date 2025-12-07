import { useState, useEffect } from 'react';

export function useUser() {
  const [user, setUser] = useState<{ name: string; initials: string }>({ name: 'Guest', initials: 'G' });

  useEffect(() => {
    // Try to get user details from localStorage
    const storedUser = localStorage.getItem('focusdeck_user');

    if (storedUser) {
      // Assuming 'focusdeck_user' stores the User ID or Name
      // Ideally we would have a full user object, but based on SignInPage, it stores the ID.
      // Let's format it.
      const name = storedUser;
      const initials = name.slice(0, 2).toUpperCase();
      setUser({ name, initials });
    }
  }, []);

  return user;
}
