/**
 * ThreeJsIntegration — Blazor-Three.js interop module
 * Manages Three.js scene lifecycle and exposes API for Blazor IJSRuntime.
 */

// Store active viewer instances keyed by container element id
const _viewers = {};

/**
 * Creates and returns a Three.js viewer instance in the given container.
 * @param {string} containerId - The id of the div element to host the canvas.
 * @param {object} options - Viewer configuration.
 * @returns {Promise<void>}
 */
export async function initViewer(containerId, options = {}) {
  await ensureThreeReady();

  if (_viewers[containerId]) {
    disposeViewer(containerId);
  }

  const container = document.getElementById(containerId);
  if (!container) {
    throw new Error(`Container element #${containerId} not found`);
  }

  const THREE = window.THREE;

  const cfg = {
    backgroundColor: options.backgroundColor || '#cccccc',
    showGrid: options.showGrid !== false,
    showAxes: options.showAxes !== false,
    cameraPosition: options.cameraPosition || [60, 40, 80],
    enableShadows: options.enableShadows !== false,
    ...options,
  };

  // Scene
  const scene = new THREE.Scene();
  scene.background = new THREE.Color(cfg.backgroundColor);

  // Camera
  const camera = new THREE.PerspectiveCamera(
    45,
    container.clientWidth / Math.max(container.clientHeight, 1),
    0.1,
    5000
  );
  camera.position.set(...cfg.cameraPosition);

  // Renderer
  const renderer = new THREE.WebGLRenderer({ antialias: true });
  renderer.setPixelRatio(Math.min(window.devicePixelRatio, 2));
  renderer.setSize(container.clientWidth, container.clientHeight);
  renderer.shadowMap.enabled = cfg.enableShadows;
  container.appendChild(renderer.domElement);

  // Lights
  scene.add(new THREE.AmbientLight(0x606060, 1.5));
  const d1 = new THREE.DirectionalLight(0xffffff, 2);
  d1.position.set(100, 100, 50);
  scene.add(d1);
  const d2 = new THREE.DirectionalLight(0x8899bb, 0.6);
  d2.position.set(-50, -30, -100);
  scene.add(d2);

  // Grid
  if (cfg.showGrid) {
    scene.add(new THREE.GridHelper(200, 20, 0x333355, 0x222244));
  }

  // Axes
  if (cfg.showAxes) {
    scene.add(new THREE.AxesHelper(50));
  }

  // Orbit controls
  let controls = null;
  if (window.THREE_ADDONS && window.THREE_ADDONS.OrbitControls) {
    controls = new window.THREE_ADDONS.OrbitControls(camera, renderer.domElement);
    controls.enableDamping = true;
    controls.dampingFactor = 0.08;
    controls.target.set(0, 0, 0);
  }

  // Animation loop
  const clock = new THREE.Clock();
  let animId = null;
  function animate() {
    animId = requestAnimationFrame(animate);
    if (controls) controls.update();
    renderer.render(scene, camera);
  }
  animate();

  // Resize handler
  function onResize() {
    camera.aspect = container.clientWidth / Math.max(container.clientHeight, 1);
    camera.updateProjectionMatrix();
    renderer.setSize(container.clientWidth, container.clientHeight);
  }
  window.addEventListener('resize', onResize);

  // Store instance
  const viewer = {
    scene,
    camera,
    renderer,
    controls,
    container,
    animId,
    clock,
    onResize,
    meshes: [],
    dispose() {
      cancelAnimationFrame(animId);
      window.removeEventListener('resize', onResize);
      if (controls) controls.dispose();
      renderer.dispose();
      scene.traverse(obj => {
        if (obj.geometry) obj.geometry.dispose();
        if (obj.material) {
          if (Array.isArray(obj.material)) {
            obj.material.forEach(m => m.dispose());
          } else {
            obj.material.dispose();
          }
        }
      });
      if (renderer.domElement && renderer.domElement.parentNode) {
        renderer.domElement.parentNode.removeChild(renderer.domElement);
      }
    },
  };

  _viewers[containerId] = viewer;
  return viewer;
}

/**
 * Loads a 3D model into an existing viewer.
 * @param {string} containerId
 * @param {string} url - Model file URL.
 * @param {string} type - 'stl' | 'obj' | 'gltf'. Default 'stl'.
 * @param {object} options - { color, onProgress, onError }
 * @returns {Promise<object>} - { size: {x,y,z}, vertexCount }
 */
export async function loadModel(containerId, url, type = 'stl', options = {}) {
  const viewer = _viewers[containerId];
  if (!viewer) throw new Error(`Viewer #${containerId} not initialized`);

  const THREE = window.THREE;

  // Remove existing meshes
  for (const m of viewer.meshes) {
    viewer.scene.remove(m);
    if (m.geometry) m.geometry.dispose();
    if (m.material) {
      const mats = Array.isArray(m.material) ? m.material : [m.material];
      mats.forEach(mt => mt.dispose());
    }
  }
  viewer.meshes = [];

  const color = options.color || 0xff6600;

  try {
    let object;

    switch (type.toLowerCase()) {
      case 'stl': {
        const { STLLoader } = window.THREE_ADDONS;
        const geo = await new Promise((resolve, reject) => {
          new STLLoader().load(url, resolve, options.onProgress, reject);
        });
        const mat = new THREE.MeshPhongMaterial({
          color,
          specular: 0x111111,
          shininess: 60,
          side: THREE.DoubleSide,
        });
        object = new THREE.Mesh(geo, mat);
        break;
      }

      case 'obj': {
        const { OBJLoader } = window.THREE_ADDONS;
        object = await new Promise((resolve, reject) => {
          new OBJLoader().load(url, resolve, options.onProgress, reject);
        });
        object.traverse(child => {
          if (child.isMesh) {
            child.material = new THREE.MeshPhongMaterial({
              color,
              specular: 0x111111,
              shininess: 60,
              side: THREE.DoubleSide,
            });
          }
        });
        break;
      }

      case 'gltf': {
        const { GLTFLoader, DRACOLoader } = window.THREE_ADDONS;
        const loader = new GLTFLoader();
        if (window.THREE_ADDONS.DRACOLoader) {
          const dracoLoader = new DRACOLoader();
          dracoLoader.setDecoderPath('https://www.gstatic.com/draco/versioned/decoders/1.5.7/');
          loader.setDRACOLoader(dracoLoader);
        }
        const gltf = await new Promise((resolve, reject) => {
          loader.load(url, resolve, options.onProgress, reject);
        });
        object = gltf.scene;
        break;
      }

      default:
        throw new Error(`Unsupported model type: ${type}`);
    }

    // Center and position
    object.updateMatrixWorld();
    const box = new THREE.Box3().setFromObject(object);
    const center = box.getCenter(new THREE.Vector3());
    object.position.sub(center);

    viewer.scene.add(object);
    viewer.meshes.push(object);

    if (viewer.controls) {
      viewer.controls.target.set(0, 0, 0);
    }

    const size = box.getSize(new THREE.Vector3());
    const dist = Math.max(size.x, size.y, size.z) * 1.8;
    viewer.camera.position.set(dist * 0.8, dist * 0.6, dist);
    viewer.camera.lookAt(0, 0, 0);

    return {
      size: { x: Math.round(size.x), y: Math.round(size.y), z: Math.round(size.z) },
      vertexCount: countVertices(object),
    };
  } catch (err) {
    console.error('ThreeJsIntegration: model load failed', err);
    throw err;
  }
}

/**
 * Resets camera to fit the current model.
 */
export function resetCamera(containerId) {
  const viewer = _viewers[containerId];
  if (!viewer || viewer.meshes.length === 0) return;

  const THREE = window.THREE;
  const box = new THREE.Box3();
  viewer.meshes.forEach(m => box.expandByObject(m));
  const size = box.getSize(new THREE.Vector3());
  const dist = Math.max(size.x, size.y, size.z) * 1.8;
  viewer.camera.position.set(dist * 0.8, dist * 0.6, dist);
  viewer.camera.lookAt(0, 0, 0);
  if (viewer.controls) viewer.controls.target.set(0, 0, 0);
}

/**
 * Sets background color.
 */
export function setBackgroundColor(containerId, color) {
  const viewer = _viewers[containerId];
  if (!viewer) return;
  viewer.scene.background = new window.THREE.Color(color);
}

/**
 * Disposes a viewer instance and cleans up all resources.
 */
export function disposeViewer(containerId) {
  const viewer = _viewers[containerId];
  if (!viewer) return;
  viewer.dispose();
  delete _viewers[containerId];
}

/**
 * Disposes all viewers.
 */
export function disposeAllViewers() {
  for (const id of Object.keys(_viewers)) {
    disposeViewer(id);
  }
}

// --- Helpers ---

function countVertices(object) {
  let count = 0;
  object.traverse(child => {
    if (child.geometry) {
      const pos = child.geometry.getAttribute('position');
      if (pos) count += pos.count;
    }
  });
  return count;
}

// --- Three.js bootstrapping (lazy-load via importmap) ---

let _threeReady = false;
let _threePromise = null;

export function ensureThreeReady() {
  if (_threeReady) return Promise.resolve();
  if (_threePromise) return _threePromise;

  _threePromise = new Promise((resolve, reject) => {
    // Check if importmap already set up
    if (window.THREE && window.THREE_ADDONS) {
      _threeReady = true;
      resolve();
      return;
    }

    // If importmap exists, load via dynamic import
    const importmap = document.querySelector('script[type="importmap"]');
    if (importmap) {
      Promise.all([
        import('three'),
        import('three/addons/controls/OrbitControls.js'),
      ]).then(([THREE, { OrbitControls }]) => {
        window.THREE = THREE;
        window.THREE_ADDONS = { OrbitControls };
        _threeReady = true;
        resolve();
      }).catch(reject);
      return;
    }

    // Fallback: no importmap, reject
    reject(new Error('Three.js importmap not found. Ensure the importmap script is included in the page.'));
  });

  return _threePromise;
}

// Expose on window for Blazor IJSRuntime interop
if (typeof window !== 'undefined') {
  window.ThreeJsIntegration = {
    initViewer,
    loadModel,
    resetCamera,
    setBackgroundColor,
    disposeViewer,
    disposeAllViewers,
    ensureThreeReady,
  };
}
