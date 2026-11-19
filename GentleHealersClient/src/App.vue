<template>
  <div class="app-shell">
    <a
      v-if="showNavigation"
      class="skip-link"
      href="#page-title"
      @click.prevent="focusPageTitle"
    >
      Ga naar de hoofdinhoud
    </a>

    <TopNavigation
      v-if="showNavigation"
      :profile-name="profileName"
      @logout="handleLogout"
    >
      <template #menu="{ close }">
        <RouterLink class="nav-menu-link" to="/" @click="close">
          <span>Home</span>
          <svg class="nav-home-icon" viewBox="0 0 24 24" aria-hidden="true" focusable="false">
            <path d="M12 3 2 11.5h3V21h6v-6h2v6h6v-9.5h3L12 3z" />
          </svg>
        </RouterLink>

        <details
          v-for="section in roleMenuSections"
          :key="section.label"
          class="nav-menu-group"
        >
          <summary>{{ section.label }}</summary>
          <div class="nav-submenu">
            <RouterLink
              v-for="item in section.items"
              :key="item.to"
              class="nav-menu-link"
              :to="item.to"
              @click="close"
            >
              {{ item.label }}
            </RouterLink>
          </div>
        </details>
      </template>

      <template #profile-panel="{ logout }">
        <div class="profile-details">
          <p>{{ profileName }}</p>
          <button type="button" class="profile-logout" @click="logout">
            Uitloggen
          </button>
        </div>
      </template>
    </TopNavigation>

    <RouterView />
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import TopNavigation from '@/components/TopNavigation.vue'
import { parseJwt } from '@/services/JwtParser.js'

const route = useRoute()
const router = useRouter()

const claims = computed(() => {
  const currentPath = route.fullPath
  const token = localStorage.getItem('token')
  // Depend on route changes so a newly stored token is picked up after login.
  if (!token || !currentPath) return null
  return parseJwt(token)
})

const userRole = computed(() => claims.value?.role ?? '')
const profileName = computed(() => {
  const firstName = claims.value?.firstName ?? claims.value?.given_name
  const lastName = claims.value?.lastName ?? claims.value?.family_name
  const fullName = [firstName, lastName].filter(Boolean).join(' ')

  // Current JWTs contain email but not first/last name, so use email until
  // the API includes name claims or provides a current-user endpoint.
  return fullName || claims.value?.email || 'Gebruiker'
})

const roleMenuSections = computed(() => {
  const menus = {
    SystemAdministrator: [
      {
        label: 'Gebruikersbeheer',
        items: [{ label: 'Rollen en rechten', to: '/rolManagement' }],
      },
    ],
    GeneralPractitioner: [
      {
        label: 'Patiënten',
        items: [{ label: 'Patiënt toevoegen', to: '/patient/aanmaken' }],
      },
    ],
  }

  return menus[userRole.value] ?? []
})

const showNavigation = computed(() =>
  Boolean(claims.value) && !['login', 'accessDenied'].includes(route.name),
)

function handleLogout() {
  localStorage.removeItem('token')
  localStorage.removeItem('userRole')
  router.push({ name: 'login' })
}

function focusPageTitle() {
  document.getElementById('page-title')?.focus()
}
</script>

<style scoped>
.app-shell {
  min-height: 100vh;
  background: #eaf3fc;
}

.skip-link {
  position: fixed;
  z-index: 100;
  top: 8px;
  left: 8px;
  transform: translateY(-150%);
  padding: 10px 14px;
  border-radius: 6px;
  background: white;
  color: #153b70;
  font-weight: 700;
}

.skip-link:focus {
  transform: translateY(0);
}

.profile-details p {
  margin: 8px 0 16px;
  font-size: 14px;
}

.profile-logout {
  width: 100%;
  min-height: 40px;
  border: 0;
  border-radius: 8px;
  background: #0878d1;
  color: white;
  font: inherit;
  cursor: pointer;
}

.profile-logout:focus-visible {
  outline: 3px solid #153b70;
  outline-offset: 3px;
}

.nav-home-icon {
  width: 24px;
  height: 24px;
  fill: #153b70;
}
</style>
