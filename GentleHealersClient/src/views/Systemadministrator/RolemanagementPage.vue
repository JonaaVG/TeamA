<template>
  <div class="app">
    <div class="content">
      <h1 id="page-title" tabindex="-1">Rollen en rechten</h1>
      <p class="intro">Wijzig de rol van een bestaand gebruikersaccount.</p>

      <StatusMessage variant="success" :empty="!successMessage">
        {{ successMessage }}
      </StatusMessage>
      <StatusMessage variant="error" :empty="!errorMessage">
        {{ errorMessage }}
      </StatusMessage>

      <div class="search-bar">
        <label for="user-search">Zoek gebruiker op naam</label>
        <input
          id="user-search"
          v-model="searchQuery"
          type="search"
          placeholder="Typ een voor- of achternaam"
          autocomplete="off"
        />
      </div>

      <section class="user-list" aria-labelledby="users-heading">
        <h2 id="users-heading" class="visually-hidden">Gebruikers en huidige rollen</h2>
        <div class="list-heading">
          <span>Gebruiker</span>
          <span>Huidige rol</span>
          <span>Accountstatus</span>
          <span></span>
          <span></span>
        </div>

        <div v-for="user in filteredUsers" :key="user.email" class="user-row">
          <div class="user-info">
            <span>{{ user.firstName }} {{ user.lastName }}</span>
            <span>{{ user.email }}</span>
          </div>

          <select v-model="user.roleId" :aria-label="`Rol voor ${user.firstName} ${user.lastName}`">
          <option v-for="role in roles" :key="role.roleId" :value="role.roleId">
            {{ role.name }}
          </option>
        </select>

        <span
          class="status-badge"
          :class="`status-${user.status?.toLowerCase()}`"
        >
          {{ statusLabel(user.status) }}
        </span>

        <ActionButton
          variant="primary"
          :aria-label="`Rol wijzigen voor ${user.firstName} ${user.lastName}`"
          @click="changeRole(user)"
        >
          Rol wijzigen
        </ActionButton>
          <ActionButton
            :variant="user.status === 'Active' ? 'danger' : 'primary'"
            :disabled="updatingUserId === user.userId"
            @click="toggleAccountStatus(user)"
          >
            {{
              updatingUserId === user.userId
                ? 'Bezig...'
                : user.status === 'Active'
                  ? 'Account deactiveren'
                  : 'Account activeren'
            }}
          </ActionButton>
        </div>

        <p v-if="filteredUsers.length === 0" class="empty-state">
          {{ searchQuery.trim() ? 'Geen gebruikers gevonden met deze naam.' : 'Er zijn geen gebruikers beschikbaar.' }}
        </p>
      </section>
    </div>
  </div>
</template>

<script setup>
import { computed, onMounted, ref } from 'vue'
import api from '@/services/api'
import ActionButton from '@/components/ActionButton.vue'
import StatusMessage from '@/components/StatusMessage.vue'

const roles = ref([])
const users = ref([])
const searchQuery = ref('')
const errorMessage = ref('')
const successMessage = ref('')
const updatingUserId = ref(null)

const filteredUsers = computed(() => {
  const query = normalizeName(searchQuery.value.trim())
  if (!query) return users.value

  return users.value.filter((user) =>
    normalizeName(`${user.firstName ?? ''} ${user.lastName ?? ''}`).includes(query),
  )
})

function statusLabel(status) {
  const labels = {
    Active: 'Actief',
    Inactive: 'Inactief',
    Blocked: 'Geblokkeerd',
    Invited: 'Uitgenodigd',
  }

  return labels[status] ?? status ?? 'Onbekend'
}

function normalizeName(value) {
  return value
    .toLocaleLowerCase('nl')
    .normalize('NFD')
    .replace(/[\u0300-\u036f]/g, '')
}

onMounted(async () => {
  try {
    const [rolesResponse, usersResponse] = await Promise.all([
      api.get('/admin/roles/roles'),
      api.get('/admin/roles/users'),
    ])

    roles.value = rolesResponse.data
    users.value = usersResponse.data
  } catch (error) {
    errorMessage.value = 'De gebruikers of rollen konden niet worden geladen. Controleer je verbinding en vernieuw de pagina.'
    console.error(error)
  }
})

async function changeRole(user) {
  successMessage.value = ''
  errorMessage.value = ''
  const previousRoleId = roles.value.find((role) => role.name === user.roleName)?.roleId

  try {
    await api.put(`/admin/roles/user/${user.userId}/role`, {
      roleId: user.roleId,
    })

    user.roleName = roles.value.find((role) => role.roleId === user.roleId)?.name ?? user.roleName

    successMessage.value =
  `De rol van ${user.firstName} ${user.lastName} is succesvol gewijzigd.` 
  } catch (error) {
    if (previousRoleId !== undefined) user.roleId = previousRoleId
    errorMessage.value =
      error.response?.data || 'De rol kon niet worden gewijzigd. Probeer het opnieuw.'
    console.error(error)
  }
}

async function toggleAccountStatus(user) {
  successMessage.value = ''
  errorMessage.value = ''

  const isActive = user.status === 'Active'
  const action = isActive ? 'deactivate' : 'activate'
  updatingUserId.value = user.userId

  try {
    await api.put(`/admin/roles/user/${user.userId}/${action}`)

    user.status = isActive ? 'Inactive' : 'Active'
    successMessage.value = isActive
      ? `Het account van ${user.firstName} ${user.lastName} is gedeactiveerd.`
      : `Het account van ${user.firstName} ${user.lastName} is geactiveerd.`
  } catch (error) {
    errorMessage.value =
      error.response?.data || 'De accountstatus kon niet worden gewijzigd.'
  } finally {
    updatingUserId.value = null
  }
}
</script>

<style scoped>
* {
  box-sizing: border-box;
}

.app {
  width: 100vw;
  min-height: 100vh;
  margin-left: calc(50% - 50vw);
  background: #eaf3fc;
  color: #153b70;
  font-family: Arial, sans-serif;
}

.content {
  padding: 20px 11px;
}

h1 {
  margin: 0 0 6px;
  font-size: 17px;
}

.intro {
  margin: 0 0 16px;
  font-size: 10px;
}

.search-bar {
  display: flex;
  flex-direction: column;
  gap: 6px;
  max-width: 420px;
  margin-bottom: 14px;
  font-size: 12px;
  font-weight: 700;
}

.search-bar input {
  width: 100%;
  min-height: 38px;
  padding: 0 12px;
  border: 1px solid #bdcde0;
  border-radius: 8px;
  background: white;
  color: #153b70;
  font: inherit;
  font-weight: 400;
}

.search-bar input:focus {
  outline: 2px solid #1681d2;
  outline-offset: 2px;
}

.empty-state {
  margin: 0;
  padding: 18px 12px;
  color: #526b89;
  font-size: 12px;
}

.user-list {
  overflow: hidden;
  background: #fff;
  border: 1px solid #bdcde0;
  border-radius: 8px;
}

.visually-hidden {
  position: absolute;
  width: 1px;
  height: 1px;
  padding: 0;
  margin: -1px;
  overflow: hidden;
  clip: rect(0, 0, 0, 0);
  white-space: nowrap;
  border: 0;
}

.list-heading,
.user-row {
  display: grid;
  grid-template-columns: 1.2fr 1.4fr 0.9fr 1fr 1fr;
  align-items: center;
  column-gap: 16px;
  padding: 0 8px;
}

.list-heading {
  min-height: 36px;
  border-bottom: 1px solid #153b70;
  font-size: 10px;
  font-weight: 700;
}

.user-row {
  min-height: 64px;
  border-bottom: 1px solid #9eb2cc;
}

.user-row:last-child {
  border-bottom: 0;
}

.user-info {
  display: flex;
  flex-direction: column;
  gap: 8px;
  font-size: 10px;
}

select {
  width: 100%;
  height: 27px;
  padding: 0 8px;
  border: 1px solid #bdcde0;
  border-radius: 8px;
  background: white;
  color: #153b70;
  font-size: 10px;
}

.status-badge {
  display: inline-block;
  width: fit-content;
  padding: 5px 10px;
  border-radius: 999px;
  font-size: 12px;
  font-weight: 700;
}

.status-active {
  background: #dcfce7;
  color: #166534;
}

.status-inactive,
.status-blocked {
  background: #fee2e2;
  color: #991b1b;
}

.status-invited {
  background: #fef3c7;
  color: #92400e;
}

@media (max-width: 600px) {
  .list-heading {
    display: none;
  }

  .user-row {
    grid-template-columns: minmax(0, 1fr);
    gap: 10px;
    padding: 12px;
  }

  .user-info {
    overflow-wrap: anywhere;
    font-size: 14px;
  }

  select {
    min-height: 40px;
    font-size: 14px;
  }
}
</style>
