<template>
  <div id="focus-resetter" tabindex="-1" aria-hidden="true"></div>
  <div class="min-h-screen bg-gray-50 text-gray-900 font-sans flex flex-col justify-between">
    <!-- Skip link for keyboard and screen reader users -->
    <a id="skip-link" href="#page-title" @click.prevent="skipToMain"
      class="skip-link focus:not-sr-only focus:fixed focus:top-4 focus:left-4 focus:z-50 focus:bg-white focus:text-blue-900 focus:px-4 focus:py-3 focus:rounded-lg focus:shadow-xl focus:border-2 focus:border-blue-900 focus:font-semibold">
      Ga direct naar hoofdinhoud
    </a>

    <div>
      <!-- Global Header & Main Navigation -->
      <header class="bg-blue-900 text-white shadow-md" role="banner">
        <div class="max-w-7xl mx-auto px-4 py-4 flex flex-col md:flex-row justify-between items-center gap-4">
          <div class="flex items-center space-x-3">
            <span class="text-2xl" aria-hidden="true">🏥</span>
            <span class="text-xl font-bold tracking-wide">De Zachte Heelmeesters</span>
          </div>

          <nav aria-label="Hoofdnavigatie">
            <ul class="flex flex-wrap items-center justify-center gap-2 md:gap-3 list-none p-0 m-0">
              <li>
                <router-link to="/"
                  class="px-3.5 py-2 rounded-md font-medium text-blue-100 hover:bg-blue-800 focus:outline-none focus:ring-2 focus:ring-white"
                  active-class="bg-blue-800 text-white ring-2 ring-white/50">
                  Dashboard
                </router-link>
              </li>
              

              <!-- Logout Knop -->
              <li>
                <button 
                  @click="handleLogout"
                  class="px-3.5 py-2 rounded-md font-medium text-blue-100 hover:bg-blue-800 focus:outline-none focus:ring-2 focus:ring-white transition-colors cursor-pointer"
                  aria-label="Uitloggen uit de applicatie">
                  Uitloggen
                </button>
              </li>
            </ul>
          </nav>
        </div>
      </header>

      <!-- Dynamic page content -->
      <main id="main-content" class="max-w-7xl mx-auto px-4 py-8" tabindex="-1">
        <RouterView />
      </main>
    </div>

    <!-- Footer -->
    <footer class="bg-white border-t border-gray-200 py-6 text-center text-sm text-gray-600 mt-12" role="contentinfo">
      <p>&copy; 2026 De Zachte Heelmeesters</p>
    </footer>
  </div>
</template>

<script setup>
import { ref } from 'vue'
import { useRouter } from 'vue-router'

const router = useRouter()

// Logout - deletes stored data and sends user to login
const handleLogout = () => {
  localStorage.removeItem('token')
  localStorage.removeItem('userRole')
  router.push('/login')
}
// Function to smoothly and reliably force focus to the main content area
const skipToMain = () => {
  const pageTitle = document.getElementById('page-title')
  if (pageTitle) {
    // Geef altijd direct de focus
    pageTitle.focus()

    // Controleer of het element al volledig binnen de huidige viewport (het zichtbare scherm) valt
    const rect = pageTitle.getBoundingClientRect()
    const isVisible =
      rect.top >= 0 &&
      rect.bottom <= (window.innerHeight || document.documentElement.clientHeight)

    // Alleen automatisch scrollen als de titel NIET in beeld is
    if (!isVisible) {
      pageTitle.scrollIntoView({ behavior: 'smooth', block: 'start' })
    }
  }
}
</script>

<style>
/* De skip-link wordt standaard volledig naar boven geduwd buiten het zicht */
.skip-link {
  position: fixed;
  top: 0;
  left: 16px;
  transform: translateY(-100%);
  background: #ffffff;
  color: #1e3a8a;
  padding: 12px 16px;
  z-index: 99999;
  border-radius: 0 0 8px 8px;
  font-weight: 600;
  box-shadow: 0 4px 6px -1px rgba(0, 0, 0, 0.1);
  border: 2px solid #1e3a8a;
  transition: transform 0.2s ease-in-out;
}

/* Zodra hij focus krijgt (via Tab), schiet hij naar beneden in beeld */
.skip-link:focus {
  transform: translateY(0);
  outline: none;
}
</style>