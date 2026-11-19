<template>
  <div class="top-navigation" @keydown.esc="closePanels">
    <header class="topbar">
      <button
        ref="menuTrigger"
        class="menu-trigger"
        type="button"
        aria-controls="navigation-drawer"
        :aria-expanded="menuOpen"
        @click="toggleMenu"
      >
        <span class="hamburger" aria-hidden="true">☰</span>
        <span>Menu</span>
      </button>

      <RouterLink
        class="brand"
        to="/"
        aria-label="De Zachte Heelmeesters — naar het dashboard"
        @click="closePanels"
      >
        <slot name="brand">
          Zachte <strong>Heelmeesters</strong>
        </slot>
      </RouterLink>

      <div class="profile-area">
        <button
          ref="profileTrigger"
          class="profile-trigger"
          type="button"
          :aria-label="`Profielmenu openen voor ${props.profileName}`"
          aria-controls="profile-panel"
          :aria-expanded="profileOpen"
          @click="toggleProfile"
        >
          <span class="profile-summary">
            <slot name="profile-summary">
              {{ props.profileName }}
            </slot>
          </span>
          <span class="profile-chevron" aria-hidden="true">⌄</span>
        </button>

        <div
          v-if="profileOpen"
          id="profile-panel"
          class="profile-panel"
          role="region"
          aria-label="Profielmenu"
        >
          <button
            class="panel-close"
            type="button"
            aria-label="Profielmenu sluiten"
            @click="closeProfile"
          >
            ×
          </button>

          <slot
            name="profile-panel"
            :close="closeProfile"
            :logout="logout"
          >
            <p>{{ props.profileName }}</p>
          </slot>
        </div>
      </div>

      <button
        class="logout-trigger"
        type="button"
        aria-label="Uitloggen"
        @click="logout"
      >
        <svg
          class="logout-icon"
          viewBox="0 0 32 32"
          aria-hidden="true"
          focusable="false"
        >
          <path d="M18 5H9a3 3 0 0 0-3 3v16a3 3 0 0 0 3 3h9" />
          <path d="M14 16h14m-6-6 6 6-6 6" />
        </svg>
      </button>
    </header>

    <div
      v-if="menuOpen"
      class="drawer-backdrop"
      @click.self="closeMenu"
    >
      <aside
        id="navigation-drawer"
        ref="menuDrawer"
        class="menu-drawer"
        aria-label="Navigatiemenu"
      >
        <button
          ref="menuCloseButton"
          class="drawer-close"
          type="button"
          @click="closeMenu"
        >
          × <span>Sluiten</span>
        </button>

        <nav class="menu-content" aria-label="Hoofdnavigatie">
          <slot name="menu" :close="closeMenu" />
        </nav>
      </aside>
    </div>
  </div>
</template>

<script setup>
import { nextTick, onMounted, onUnmounted, ref } from 'vue'

const props = defineProps({
  profileName: { type: String, default: 'Naam' },
})

const emit = defineEmits(['logout'])

const menuOpen = ref(false)
const profileOpen = ref(false)
const menuTrigger = ref(null)
const menuCloseButton = ref(null)
const profileTrigger = ref(null)

function toggleMenu() {
  menuOpen.value = !menuOpen.value
  profileOpen.value = false

  if (menuOpen.value) {
    nextTick(() => menuCloseButton.value?.focus())
  }
}

function closeMenu() {
  menuOpen.value = false
  nextTick(() => menuTrigger.value?.focus())
}

function toggleProfile() {
  profileOpen.value = !profileOpen.value
  menuOpen.value = false
}

function closeProfile() {
  profileOpen.value = false
  nextTick(() => profileTrigger.value?.focus())
}

function closePanels() {
  if (menuOpen.value) closeMenu()
  if (profileOpen.value) closeProfile()
}

function logout() {
  emit('logout')
}

function handleEscape(event) {
  if (event.key === 'Escape') closePanels()
}

onMounted(() => window.addEventListener('keydown', handleEscape))
onUnmounted(() => window.removeEventListener('keydown', handleEscape))
</script>

<style scoped>
.topbar {
  position: relative;
  z-index: 2;
  display: grid;
  grid-template-columns: minmax(120px, 1fr) minmax(160px, 1.5fr) minmax(145px, 1fr) 54px;
  height: 60px;
  background: #fff;
  color: #153b70;
  border: 1px solid #c4d1e0;
  border-radius: 0 0 8px 8px;
  box-shadow: 0 2px 4px #153b7020;
  font-family: Arial, sans-serif;
}

.menu-trigger,
.profile-trigger,
.logout-trigger,
.drawer-close,
.panel-close {
  border: 0;
  background: transparent;
  color: inherit;
  cursor: pointer;
  font: inherit;
}

.menu-trigger {
  display: flex;
  align-items: center;
  gap: 12px;
  padding: 0 16px;
  font-weight: 700;
}

.hamburger {
  font-size: 24px;
  line-height: 1;
}

.brand {
  align-self: center;
  text-align: center;
  font-size: 17px;
  font-weight: 700;
  white-space: nowrap;
  color: inherit;
  text-decoration: none;
}

.brand :deep(strong) {
  color: #0b65ad;
}

.brand:focus-visible {
  outline: 3px solid #1681d2;
  outline-offset: 2px;
  border-radius: 2px;
}

.profile-area {
  position: relative;
  border-left: 1px solid #c4d1e0;
  border-right: 1px solid #c4d1e0;
}

.profile-trigger {
  display: flex;
  width: 100%;
  height: 100%;
  align-items: center;
  justify-content: space-between;
  padding: 6px 16px;
  text-align: left;
}

.profile-summary {
  font-size: 14px;
  font-weight: 600;
  line-height: 1.2;
}

.profile-chevron {
  flex: 0 0 auto;
  margin-left: 10px;
  color: #153b70;
  font-size: 22px;
  font-weight: 700;
  line-height: 1;
}

.logout-trigger {
  display: flex;
  align-items: center;
  justify-content: center;
}

.logout-icon {
  width: 32px;
  height: 32px;
  fill: none;
  stroke: currentColor;
  stroke-width: 2.2;
  stroke-linecap: round;
  stroke-linejoin: round;
}

.menu-trigger:focus-visible,
.profile-trigger:focus-visible,
.logout-trigger:focus-visible,
.drawer-close:focus-visible,
.panel-close:focus-visible {
  outline: 3px solid #1681d2;
  outline-offset: -3px;
}

.drawer-backdrop {
  position: fixed;
  z-index: 10;
  inset: 0;
  background: #0003;
}

.menu-drawer {
  width: min(292px, 86vw);
  min-height: 100%;
  background: #fff;
  color: #153b70;
  box-shadow: 2px 0 8px #0002;
}

.drawer-close {
  display: flex;
  width: 100%;
  height: 60px;
  align-items: center;
  gap: 12px;
  padding: 0 20px;
  border-bottom: 1px solid #bdcde0;
  font-weight: 700;
}

.menu-content {
  display: flex;
  flex-direction: column;
}

.menu-content :deep(.nav-menu-link) {
  display: flex;
  min-height: 40px;
  align-items: center;
  justify-content: space-between;
  padding: 8px 20px;
  border-bottom: 1px solid #bdcde0;
  color: #153b70;
  font-weight: 700;
  text-decoration: none;
}

.menu-content :deep(.nav-menu-link:hover),
.menu-content :deep(.nav-menu-link:focus-visible) {
  background: #eaf3fc;
}

.menu-content :deep(.nav-menu-group) {
  border-bottom: 1px solid #bdcde0;
}

.menu-content :deep(.nav-menu-group summary) {
  padding: 12px 20px;
  color: #153b70;
  font-weight: 700;
  cursor: pointer;
}

.menu-content :deep(.nav-menu-group summary:focus-visible) {
  outline: 3px solid #1681d2;
  outline-offset: -3px;
}

.menu-content :deep(.nav-submenu .nav-menu-link) {
  padding-left: 36px;
  font-weight: 400;
}

.profile-panel {
  position: absolute;
  z-index: 5;
  top: calc(100% + 4px);
  right: 4px;
  width: min(230px, 85vw);
  min-height: 120px;
  padding: 16px;
  border: 1px solid #bdcde0;
  border-radius: 8px;
  background: white;
  box-shadow: 0 4px 12px #0002;
}

.panel-close {
  float: right;
  font-size: 22px;
}

@media (max-width: 600px) {
  .topbar {
    grid-template-columns: 66px minmax(0, 1fr) minmax(96px, 0.8fr) 42px;
  }

  .menu-trigger {
    gap: 6px;
    padding: 0 8px;
    font-size: 13px;
  }

  .hamburger {
    font-size: 21px;
  }

  .brand {
    overflow: hidden;
    font-size: clamp(11px, 3.5vw, 15px);
    text-overflow: ellipsis;
  }

  .profile-trigger {
    gap: 4px;
    padding: 5px 7px;
  }

  .profile-summary {
    min-width: 0;
    overflow: hidden;
    font-size: 11px;
    text-overflow: ellipsis;
    white-space: nowrap;
  }

  .profile-chevron {
    margin-left: 2px;
    font-size: 19px;
  }

  .logout-icon {
    width: 27px;
    height: 27px;
  }

  .menu-drawer {
    width: 100vw;
  }

  .profile-panel {
    position: fixed;
    top: 4px;
    right: 4px;
    width: min(320px, calc(100vw - 8px));
  }
}

@media (max-width: 380px) {
  .topbar {
    grid-template-columns: 58px minmax(0, 1fr) minmax(90px, 0.8fr) 38px;
  }

  .menu-trigger {
    gap: 4px;
    padding: 0 5px;
    font-size: 12px;
  }

  .brand {
    font-size: clamp(10px, 3.4vw, 12px);
  }

  .profile-summary {
    font-size: 10px;
  }
}
</style>
