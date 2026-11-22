<template>
  <div class="app-shell">
    <a id="skip-link" href="#page-title" @click.prevent="skipToMain"
      class="skip-link focus:not-sr-only focus:fixed focus:top-4 focus:left-4 focus:z-50 focus:bg-white focus:text-blue-900 focus:px-4 focus:py-3 focus:rounded-lg focus:shadow-xl focus:border-2 focus:border-blue-900 focus:font-semibold">
      Ga direct naar hoofdinhoud
    </a>

    <TopNavigation
      v-if="showNavigation"
      :profile-name="profileName"
      :profile-role="profileRole"
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

        <RouterLink
          v-if="canImportTreatmentCatalog"
          class="nav-menu-link"
          :to="{ name: 'treatment-code-import' }"
          @click="close"
        >
          {{ nl.TreatmentCodeImportPage.title }}
        </RouterLink>
      </template>

      <template #profile-panel="{ logout }">
        <div class="profile-details">
          <p class="profile-identity">
            <span>{{ profileName }}</span>
            <span class="profile-identity-role">{{ profileRole }}</span>
          </p>
          <button type="button" class="profile-logout" @click="logout">
            Uitloggen
          </button>
        </div>
      </template>
    </TopNavigation>

    <div id="main-content">
      <RouterView />
    </div>
  </div>
</template>

<script setup>
import { computed } from 'vue'
import { useRoute, useRouter } from 'vue-router'
import TopNavigation from '@/components/TopNavigation.vue'
import { canImportTreatmentCodes } from '@/features/treatmentCodes/access'
import nl from '@/features/treatmentCodes/nl.json'
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
const profileRole = computed(() => {
  const labels = {
    Patient: 'Patiënt',
    GeneralPractitioner: 'Huisarts',
    Specialist: 'Specialist',
    Nurse: 'Verpleegkundige',
    AdministrativeStaff: 'Administratief medewerker',
    SystemAdministrator: 'Systeembeheerder',
    HealthInsurer: 'Zorgverzekeraar',
  }

  return labels[userRole.value] ?? userRole.value
})
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
        items: [{ label: 'Rollen en rechten', to: '/rol/management' }],
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

const canImportTreatmentCatalog = computed(() =>
  Boolean(claims.value) && canImportTreatmentCodes(localStorage.getItem('token')),
)

function handleLogout() {
  localStorage.removeItem('token')
  localStorage.removeItem('userRole')
  router.push({ name: 'login' })
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

<style scoped>
.app-shell {
  min-height: 100vh;
  background: #eaf3fc;
}

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

.skip-link:focus {
  transform: translateY(0);
  outline: none;
}

:deep(#page-title:focus) {
  outline: 3px solid #1681d2;
  outline-offset: 4px;
}

.profile-details p {
  margin: 8px 0 16px;
  font-size: 14px;
}

.profile-identity {
  display: flex;
  flex-direction: column;
  gap: 4px;
}

.profile-identity-role {
  color: #153b70;
  font-size: 12px;
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
