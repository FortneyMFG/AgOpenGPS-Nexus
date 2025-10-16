package hal

type Config struct {
	PWM PWMConfig `yaml:"pwm"`
}

type PWMConfig struct {
	Chip     int `yaml:"chip"`
	Channel  int `yaml:"channel"`
	PeriodNS int `yaml:"period_ns"`
}

type HAL interface {
	Init(cfg Config) error
	SetPWM(dutyNS uint32) error
	SetGPIO(on bool) error
	SendCAN(id uint32, data []byte) error
}
