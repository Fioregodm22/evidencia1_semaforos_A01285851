import agentpy as ap
import socket
import time
#Fiorella de Medina Castro A01285851

# clase para el semaforo 
class TrafficLight(ap.Agent):
    def setup(self, period=15):
        # estados del semaforo y duracion de cada uno
        self.states = ["verde", "amarillo", "rojo"]
        self.durations = {
            "verde": period * 0.4,
            "amarillo": period * 0.1,
            "rojo": period * 0.5
        }
        self.state = "rojo"  # estado inicial
        self.timer = 0       # contador de tiempo
        self.position = None 

    def step(self):
        # avanza el tiempo y cambia de estado si se cumple la duracion
        self.timer += 1
        if self.timer >= self.durations[self.state]:
            idx = self.states.index(self.state)
            self.state = self.states[(idx + 1) % len(self.states)]
            self.timer = 0

# clase para el carro 
class Car(ap.Agent):
    def setup(self, speed=1):
        self.position = 0
        self.speed = speed
        self.passed_traffic_light = False  # indica si ya paso el semaforo

    def step(self):
        light = self.model.traffic_light[0]
        stop_position = light.position

        if not self.passed_traffic_light:
            if self.position + self.speed >= stop_position:
                # va a alcanzar el semaforo
                if light.state == "rojo":
                    # se detiene justo frente al semaforo
                    self.position = stop_position
                else:
                    # puede pasar el semaforo
                    self.position += self.speed
                    self.passed_traffic_light = True
            else:
                # se mueve antes de llegar al semaforo
                self.position += self.speed
        else:
            # ya paso el semaforo, sigue avanzando
            self.position += self.speed

# clase del modelo 
class RoadModel(ap.Model):
    def setup(self):
        p = self.p  # parametros 

        # crea el semaforo con periodo especificado
        self.traffic_light = ap.AgentList(self, 1, TrafficLight, period=p['light_period'])
        self.traffic_light[0].position = 14  # posicion fija del semaforo

        # crea el carro con velocidad especificada
        self.car = ap.AgentList(self, 1, Car, speed=p['speed'])

    def step(self):
        # ejecuta un paso de simulacion para cada agente
        self.traffic_light.step()
        self.car.step()


def run_simulation_and_send(parameters):
    # crea socket tcp y se conecta al servidor en unity
    s = socket.socket(socket.AF_INET, socket.SOCK_STREAM)
    s.connect(("127.0.0.1", 1107))
    print("conectado a unity")

    # inicializa el modelo con los parametros
    model = RoadModel(parameters)
    model.setup()

    # ejecuta la simulacion por el numero de pasos definido
    for _ in range(parameters['steps']):
        model.step()

        # obtiene posicion del carro y estado del semaforo
        car_pos = model.car[0].position
        light_state = model.traffic_light[0].state

        # construye mensaje para unity
        msg = f"CAR,{car_pos:.2f};LIGHT,{light_state};"
        s.sendall(msg.encode("ascii"))  # envia por tcp

        print("enviado:", msg)

        time.sleep(0.5)  # espera medio segundo entre pasos

    s.close()  # cierra la conexion


parameters = {
    'light_period': 14.70,
    'speed': 0.7,
    'road_length': 30,
    'steps': 100
}

run_simulation_and_send(parameters)
